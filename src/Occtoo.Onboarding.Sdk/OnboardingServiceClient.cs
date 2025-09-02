using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Models;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Occtoo.Onboarding.Sdk
{
    public class OnboardingServiceClient : IOnboardingServiceClient, IDisposable
    {
        private static readonly HttpClient httpClient = new HttpClient(new HttpRetryMessageHandler(new HttpClientHandler()))
        {
            BaseAddress = new Uri("https://ingest.occtoo.com")
        };
        private readonly string cachekey = "token";
        private readonly string dataProviderId;
        private readonly string dataProviderSecret;
        private readonly IMemoryCache cache;

        public OnboardingServiceClient(string dataProviderId, string dataProviderSecret)
        {
            this.dataProviderId = dataProviderId;
            this.dataProviderSecret = dataProviderSecret;
            cache = new MemoryCache(new MemoryCacheOptions());
        }

        public StartImportResponse StartEntityImport(string dataSource, IReadOnlyList<DynamicEntity> entities, string token = null, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            return StartEntityImportAsync(dataSource, entities, token, correlationId, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token = null, Guid? correlationId = null, CancellationToken? cancellationToken = null)
        {
            var validEntities = ValidateParametes(dataSource, entities, cancellationToken);
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var response = await EntityImportAsync(dataSource, validEntities, token, valueOrDefaultCancelToken, correlationId);
            return response;
        }

        public string GetToken(CancellationToken? cancellationToken = null)
        {
            return GetTokenAsync(cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<string> GetTokenAsync(CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "dataProviders/tokens")
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    id = dataProviderId,
                    secret = dataProviderSecret
                }), Encoding.UTF8, "application/json")
            };
            var tokenResponse = await httpClient.SendAsync(tokenRequest, valueOrDefaultCancelToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new ArgumentException("Couldn't obtain a token please check your dataprovider details");
            }

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenDocument = JsonConvert.DeserializeObject<TokenResponse>(tokenResponseContent);
            return tokenDocument.result.accessToken;
        }

        public ApiResult<MediaFileDto> GetFile(string fileId, string token = null, CancellationToken? cancellationToken = null)
        {
            return GetFileAsync(fileId, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<MediaFileDto>> GetFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Get, $"media/files/{fileId}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            return await GetApiResultFromResponse<MediaFileDto>(response);
        }

        public ApiResult<MediaFileDto> GetFileFromUniqueId(string UniqueIdentifier, string token = null, CancellationToken? cancellationToken = null)
        {
            return GetFileFromUniqueIdAsync(UniqueIdentifier, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<MediaFileDto>> GetFileFromUniqueIdAsync(string UniqueIdentifier, string token = null, CancellationToken? cancellationToken = null)
        {
            var mediaFileDto = new MediaFileDto();
            var response = await GetFilesBatchAsync(new List<string> { UniqueIdentifier }, token, cancellationToken);
            if(response.Errors.Any())
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = response.Errors,
                    StatusCode = response.StatusCode,
                    Result = mediaFileDto
                };
            }

            if (response.Result.Failures.Any())
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[] { new Error("MediaFile not found in tenant") },
                    StatusCode = 404,
                    Result = mediaFileDto
                };
            }

            return new ApiResult<MediaFileDto>
            {
                Errors = response.Errors,
                StatusCode = response.StatusCode,
                Result = response.Result.Succeeded.First().Value
            };
        }

        public ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>> GetFilesBatch(List<string> uniqueIdentifiers, string token = null, CancellationToken? cancellationToken = null)
        {
            return GetFilesBatchAsync(uniqueIdentifiers, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(List<string> uniqueIdentifiers, string token = null, CancellationToken? cancellationToken = null)
        {
            var content = new GetMediaByUniqueIdentifiers { UniqueIdentifiers = uniqueIdentifiers };
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Post, "media/files/batch")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" },
                },
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            return await GetApiResultFromResponse<PartialSuccessResponse<string, MediaFileDto, Error>>(response); ;
        }

        public ApiResult<PartialSuccessResponse<string, UploadDto, Error>> UploadFromLinks(List<FileUploadFromLink> links, string token = null, CancellationToken? cancellationToken = null)
        {
            return UploadFromLinksAsync(links, token, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initiates asynchronous upload of files using URL to them. 
        /// Since the upload is asynchronous the client should periodiacally 
        /// check it's state using GetUploadStatusAsync method.
        /// Will skip file if UniqueIdentifier on the file already exists.
        /// </summary>
        /// <param name="links">List of links to upload</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        public async Task<ApiResult<PartialSuccessResponse<string, UploadDto, Error>>> UploadFromLinksAsync(List<FileUploadFromLink> links, string token = null, CancellationToken? cancellationToken = null)
        {
            var content = new UploadLinksRequest(links);
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Put, "media/uploads/links")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" },
                },
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            return await GetApiResultFromResponse<PartialSuccessResponse<string, UploadDto, Error>>(response);
        }

        public ApiResult<MediaFileDto> UploadFromLink(FileUploadFromLink link, string token = null, CancellationToken? cancellationToken = null)
        {
            return UploadFromLinkAsync(link, token, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initiates asynchronous upload of a file using the URL to it. 
        /// Will skip file if UniqueIdentifier on the file already exists.
        /// </summary>
        /// <param name="link">link to upload</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        public async Task<ApiResult<MediaFileDto>> UploadFromLinkAsync(FileUploadFromLink link, string token = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(link.UniqueIdentifier))
            {
                return new ApiResult<MediaFileDto> { StatusCode = 400, Errors = new Error[1] { new Error("UniqueIdentifyer can not be null or empty") } };
            }

            var uploadResponse = await UploadFromLinksAsync(new List<FileUploadFromLink> { link }, token, cancellationToken);
            if (uploadResponse.StatusCode != 202)
            {
                return new ApiResult<MediaFileDto> { StatusCode = uploadResponse.StatusCode, Errors = uploadResponse.Errors };
            }

            var fileRequest = await GetFileFromUniqueIdAsync(link.UniqueIdentifier, token, cancellationToken);
            if (fileRequest.StatusCode != 200)
            {
                return new ApiResult<MediaFileDto> { StatusCode = fileRequest.StatusCode, Errors = fileRequest.Errors };
            }

            return fileRequest;
        }

        public ApiResult<UploadDto> GetUploadStatus(string uploadId, string token = null, CancellationToken? cancellationToken = null)
        {
            return GetUploadStatusAsync(uploadId, token, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the upload information and state using the upload id
        /// </summary>
        /// <param name="uploadId">Id of the upload to check</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        public async Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Get, $"media/uploads/{uploadId}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            return await GetApiResultFromResponse<UploadDto>(response);
        }

        public ApiResult DeleteFile(string fileId, string token = null, CancellationToken? cancellationToken = null)
        {
            return DeleteFileAsync(fileId, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult> DeleteFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Delete, $"media/files/{fileId}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
                }
            };
            var response = await httpClient.SendAsync(message, valueOrDefaultCancelToken);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult
                {
                    StatusCode = (int)response.StatusCode
                };
            }
            else
            {
                var apiResult = JsonConvert.DeserializeObject<ApiResult>(await response.Content.ReadAsStringAsync());
                apiResult.StatusCode = (int)response.StatusCode;
                return apiResult;
            }
        }

        public ApiResult<MediaFileDto> UploadFile(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null)
        {
            return UploadFileAsync(content, metadata, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null)
        {
            var fileResponse = await CreateFileAsync((int)metadata.Size, UploadMetadata.Serialize(metadata).Value, token, cancellationToken);
            if (!fileResponse.IsSuccessStatusCode)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { new Error(await fileResponse.Content.ReadAsStringAsync()) },
                    StatusCode = 409
                };
            }

            var fileId = GetFileId(fileResponse);
            if (fileId.IsFailure)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { fileId.Error },
                    StatusCode = 500
                };
            }

            var uploadResponse = await CreateObservableUpload(fileId.Value, content, 0L, token, cancellationToken).LastOrDefaultAsync();
            if (!uploadResponse.IsCompleted)
            {
                return new ApiResult<MediaFileDto>
                {
                    Errors = new Error[1] { new Error($"Could only complete {uploadResponse.CompletedPercentage} percentage of the file.") },
                    StatusCode = 500
                };
            }

            return await GetFileAsync(fileId.Value, token, cancellationToken);
        }

        public ApiResult<MediaFileDto> UploadFileIfNotExist(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null)
        {
            return UploadFileIfNotExistAsync(content, metadata, token, cancellationToken).GetAwaiter().GetResult();
        }

        public async Task<ApiResult<MediaFileDto>> UploadFileIfNotExistAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(metadata.UniqueIdentifier))
            {
                return new ApiResult<MediaFileDto> { StatusCode = 400, Errors = new Error[1] { new Error("UniqueIdentifyer can not be null or empty") } };
            }

            var uploadResponse = await UploadFileAsync(content, metadata, token, cancellationToken);
            if (uploadResponse.StatusCode == 409) //File already exist
            {
                var fileRequest = await GetFileFromUniqueIdAsync(metadata.UniqueIdentifier, token, cancellationToken);
                if (fileRequest.StatusCode != 200)
                {
                    return new ApiResult<MediaFileDto> { StatusCode = fileRequest.StatusCode, Errors = fileRequest.Errors };
                }

                return fileRequest;
            }

            return uploadResponse;
        }

        private static async Task<ApiResult<T>> GetApiResultFromResponse<T>(HttpResponseMessage response)
        {
            var apiResult = new ApiResult<T>();
            if (!response.IsSuccessStatusCode)
            {
                // Getting html back in the content here so we set the apiResult ourselves
                apiResult.Errors = new Error[] { new Error("Web server received an invalid response while acting as a gateway or proxy server.") };
            }
            else
            {
                apiResult = JsonConvert.DeserializeObject<ApiResult<T>>(await response.Content.ReadAsStringAsync());
            }

            apiResult.StatusCode = (int)response.StatusCode;
            return apiResult;
        }

        private static async Task<StartImportResponse> EntityImportAsync(string dataSource, IEnumerable<DynamicEntity> validEntities, string token, CancellationToken cancellationToken, Guid? correlationId = null)
        {
            string requestUri = $"import/{dataSource}";
            if (correlationId.HasValue && correlationId != default(Guid))
            {
                requestUri += $"?correlationId={correlationId.Value}";
            }

            var ingestRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            ingestRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ingestRequest.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                Entities = validEntities
            }), Encoding.UTF8, "application/json");
            var ingestResponse = await httpClient.SendAsync(ingestRequest, cancellationToken);
            if (!ingestResponse.IsSuccessStatusCode)
            {
                switch (ingestResponse.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        throw new UnauthorizedAccessException(ingestResponse.ReasonPhrase + ". Check your dataprovider details and datasource name");
                }
            }

            var responseContent = await ingestResponse.Content.ReadAsStringAsync();
            var response = new StartImportResponse
            {
                Result = JsonConvert.DeserializeObject<ImportBatchResult>(responseContent),
                StatusCode = (int)ingestResponse.StatusCode,
                Message = ingestResponse.ReasonPhrase
            };

            return response;
        }

        private async Task<string> GetTokenThroughCache(CancellationToken cancellationToken)
        {
            if (cache.TryGetValue<string>(cachekey, out var token))
            {
                return token;
            }

            token = await GetTokenAsync(cancellationToken);
            cache.Set(cachekey, token, DateTime.UtcNow.AddMinutes(59)); // Store data in the cache for 59 minutes from now
            return token;
        }

        private static IEnumerable<DynamicEntity> ValidateParametes(string dataSource, IReadOnlyList<DynamicEntity> entities, CancellationToken? cancellationToken)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (string.IsNullOrWhiteSpace(dataSource))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dataSource));
            }

            cancellationToken?.ThrowIfCancellationRequested();
            var validEntities = ValidatePayload(entities);
            cancellationToken?.ThrowIfCancellationRequested();
            return validEntities;
        }

        private static IEnumerable<DynamicEntity> ValidatePayload(IEnumerable<DynamicEntity> entities)
        {
            Regex InvalidCharacters = new Regex(@"^[a-zA-Z0-9_-]{1,256}$", RegexOptions.Compiled);
            Regex InvalidCharactersLang = new Regex(@"^[a-zA-Z0-9_-]{1,10}$", RegexOptions.Compiled);

            var validEntitites = entities.Where(e => e != null).Select(e =>
            {
                if (e.Properties == null)
                {
                    e.Properties = new List<DynamicProperty>();
                }

                return e;
            });

            if (validEntitites.Any(e => string.IsNullOrWhiteSpace(e.Key)))
            {
                throw new ArgumentException("Entities must not have null or empty Key identifiers.");
            }

            if (validEntitites.Any(e => !InvalidCharacters.IsMatch(e.Key)))
            {
                throw new ArgumentException("Entities must have Key identifiers containing only letters, digits, underscores, or hyphens and is at most 256 characters long .");
            }

            var groupedEntites = validEntitites.GroupBy(e => e.Key).Where(k => k.Count() > 1);
            if (groupedEntites.Any())
            {
                throw new ArgumentException($"Collection contains duplicate keys: {string.Join(",", groupedEntites.Select(e => e.Key))}.");
            }

            var groupedEntitiesOnProperties = validEntitites.Where(e => e.Properties.GroupBy(p => (p.Id, p.Language)).Any(g => g.Count() > 1));
            if (groupedEntitiesOnProperties.Any())
            {
                throw new ArgumentException($"Entities: {string.Join(",", groupedEntitiesOnProperties.Select(e => e.Key))} contain duplicated properties");
            }

            foreach (var entity in validEntitites)
            {
                foreach (var property in entity.Properties)
                {
                    if (!InvalidCharacters.IsMatch(property.Id))
                    {
                        throw new ArgumentException($"{property.Id} - Entities must have Property identifiers containing only letters, digits, underscores, or hyphens and is at most 256 characters long .");
                    }
                    if (!string.IsNullOrEmpty(property.Language) && !InvalidCharactersLang.IsMatch(property.Language))
                    {
                        throw new ArgumentException($"{property.Language} - Entities must have Property Language containing only letters, digits, underscores, or hyphens and is at most 10 characters long .");
                    }
                }
            }

            return validEntitites;
        }

        #region TUS, open protocol for resumable file uploads https://tus.io/
        /// <summary>
        /// An empty POST request is used to create a new upload resource. 
        /// The Upload-Length header indicates the size of the entire upload in bytes.
        /// </summary>
        /// <param name="contentLength">Length of the upload in bytes</param>
        /// <param name="metadata">
        /// The Upload-Metadata requestand response header MUST consist of one or more comma-separated key-valuepairs. 
        /// The key and value MUST be separated by a space. The key MUST NOTcontain spaces and commas and MUST NOT be empty. 
        /// The key SHOULD be ASCIIencoded and the value MUST be Base64 encoded. All keys MUST be unique. Thevalue MAY be empty. 
        /// In these cases, the space, which would normally separatethe key and the value, MAY be left out. Since metadata can 
        /// contain arbitrarybinary values, Servers SHOULD carefully validate metadata values or sanitizethem before using them 
        /// as header values to avoid header smuggling.
        /// </param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> CreateFileAsync(int contentLength, string metadata, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(HttpMethod.Post, "media/uploads/files")
            {
                Headers =
                {
                    {"Authorization", $"Bearer {token}" },
                    {"Tus-Resumable", "1.0.0"},
                    {"Upload-Length", contentLength.ToString()},
                    {"Upload-Metadata", metadata},
                    {"Upload-Offset", "0"}
                }
            };
            return await httpClient.SendAsync(message, valueOrDefaultCancelToken);
        }

        /// <summary>
        /// All PATCH requests MUST use Content-Type: application/offset+octet-stream, 
        /// otherwise the server SHOULD return a 415 Unsupported Media Type status.'
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="bufferLength">Length of the buffer</param>
        /// <param name="currentOffset">Current offset</param>
        /// <param name="memoryStream">The stream to patch with</param>
        /// <param name="cancellationToken">Own cancellation token can be provided</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> PatchFileAsync(string fileId, int bufferLength, long currentOffset, MemoryStream memoryStream, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenThroughCache(valueOrDefaultCancelToken);
            }

            var message = new HttpRequestMessage(new HttpMethod("patch"), $"media/uploads/files/{fileId}")
            {
                Headers =
                {
                    {"Authorization", $"Bearer {token}" },
                    {"Tus-Resumable", "1.0.0"},
                    {"Upload-Offset", currentOffset.ToString()},
                },
                Content = new StreamContent(memoryStream, bufferLength)
                {
                    Headers = { { "Content-Type", "application/offset+octet-stream" } }
                }
            };
            return await httpClient.SendAsync(message, valueOrDefaultCancelToken);
        }

        private IObservable<Progress> CreateObservableUpload(string fileId, Stream content, long offset, string token = null, CancellationToken? cancellationToken = null)
        {
            CancellationToken valueOrDefaultCancelToken = cancellationToken.GetValueOrDefault();
            int chunkSize = 4194304; // 4mb
            long currentOffset = offset;
            var observable = Observable.Create<Progress>(async observer =>
            {
                while (currentOffset < content.Length)
                {
                    var buffer = new byte[Math.Min(chunkSize, content.Length - currentOffset)];
                    var bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, valueOrDefaultCancelToken);
                    HttpResponseMessage patchResponse = await PatchFileAsync(
                        fileId,
                        buffer.Length,
                        currentOffset,
                        new MemoryStream(buffer),
                        token,
                        valueOrDefaultCancelToken);
                    currentOffset = Int32.Parse(patchResponse.Headers.GetValues("Upload-Offset").First());
                    observer.OnNext(new Progress(content.Length, currentOffset, (currentOffset / content.Length) * 100,
                        content.Length == currentOffset));
                }

                observer.OnCompleted();
            });
            return observable;
        }

        private static Result<string, Error> GetFileId(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Location", out var location))
                return location.First().Split('/').Last();
            return Result.Failure<string, Error>(new Error("Upload failed. File creation response does not contain file location in header"));
        }
        #endregion

        public void Dispose() => httpClient?.Dispose();
    }
}