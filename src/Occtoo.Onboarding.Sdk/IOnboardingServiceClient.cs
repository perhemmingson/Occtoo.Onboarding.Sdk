using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk
{
    public interface IOnboardingServiceClient
    {
        //Asynchronous
        Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, string token = null, Guid? correlationId = null, CancellationToken? cancellationToken = null);
        Task<string> GetTokenAsync(CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> GetFileFromUniqueIdAsync(string UniqueIdentifier, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(List<string> identifiers, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFromLinkAsync(FileUploadFromLink link, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<PartialSuccessResponse<string, UploadDto, Error>>> UploadFromLinksAsync(List<FileUploadFromLink> links, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<MediaFileDto>> UploadFileIfNotExistAsync(Stream content, UploadMetadata metadata, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, string token = null, CancellationToken? cancellationToken = null);
        Task<ApiResult> DeleteFileAsync(string fileId, string token = null, CancellationToken? cancellationToken = null);
    }
}