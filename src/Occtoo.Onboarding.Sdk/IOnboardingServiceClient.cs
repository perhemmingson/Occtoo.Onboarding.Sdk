using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Occtoo.Onboarding.Sdk;
public interface IOnboardingServiceClient
{
	Task<StartImportResponse> StartEntityImportAsync(string dataSource, IReadOnlyList<DynamicEntity> entities, Guid? correlationId = null, CancellationToken cancellationToken = default);
	Task<ApiResult<MediaFileDto>> GetFileAsync(string fileId, CancellationToken cancellationToken = default);
	Task<ApiResult<MediaFileDto>> GetFileFromUniqueIdAsync(string UniqueIdentifier, CancellationToken cancellationToken = default);
	Task<ApiResult<PartialSuccessResponse<string, MediaFileDto, Error>>> GetFilesBatchAsync(List<string> uniqueIdentifiers, CancellationToken cancellationToken = default);

	/// <summary>
	/// Initiates asynchronous upload of files using URL to them. 
	/// Since the upload is asynchronous the client should periodiacally 
	/// check it's state using GetUploadStatusAsync method.
	/// Will skip file if UniqueIdentifier on the file already exists.
	/// </summary>
	/// <param name="links">List of links to upload</param>
	/// <param name="cancellationToken">Own cancellation token can be provided</param>
	/// <returns></returns>
	Task<ApiResult<PartialSuccessResponse<string, UploadDto, Error>>> UploadFromLinksAsync(List<FileUploadFromLink> links, CancellationToken cancellationToken = default);

	/// <summary>
	/// Initiates asynchronous upload of a file using the URL to it. 
	/// Will skip file if UniqueIdentifier on the file already exists.
	/// </summary>
	/// <param name="link">link to upload</param>
	/// <param name="cancellationToken">Own cancellation token can be provided</param>
	/// <returns></returns>
	Task<ApiResult<MediaFileDto>> UploadFromLinkAsync(FileUploadFromLink link, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves the upload information and state using the upload id
	/// </summary>
	/// <param name="uploadId">Id of the upload to check</param>
	/// <param name="cancellationToken">Own cancellation token can be provided</param>
	/// <returns></returns>
	Task<ApiResult<UploadDto>> GetUploadStatusAsync(string uploadId, CancellationToken cancellationToken = default);

	Task<ApiResult> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);
	Task<ApiResult<MediaFileDto>> UploadFileAsync(Stream content, UploadMetadata metadata, CancellationToken cancellationToken = default);
	Task<ApiResult<MediaFileDto>> UploadFileIfNotExistAsync(Stream content, UploadMetadata metadata, CancellationToken cancellationToken = default);
}
