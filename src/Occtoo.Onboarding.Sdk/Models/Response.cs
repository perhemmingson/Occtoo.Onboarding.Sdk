using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Occtoo.Onboarding.Sdk.Models
{
    public abstract class Response<T>
    {
        public int StatusCode { get; internal set; }
        public string Message { get; internal set; }
        public T Result { get; internal set; }
        public override string ToString()
        {
            return $"{StatusCode} - {Message}. {Result}";
        }
    }

    public class StartImportResponse : Response<ImportBatchResult>
    {
    }

    public class ImportBatchResult
    {
        public Guid Id { get; set; }

        public override string ToString() => string.Format("Id: {0}", Id);
    }


    public class ApiResult<T>
    {
        public T Result { get; set; }
        public Error[] Errors { get; set; }
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
    }
    public class ApiResult
    {
        public Error[] Errors { get; set; }
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
    }

    public class Error
    {
        public Error(string message) => Message = message;
        public string Message { get; set; }
    }

    public class UploadDto
    {
        public string Id { get; set; }
        public Progress Progress { get; set; }
        public UploadState State { get; set; }
        public Maybe<string> SourceUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public UploadMetadata Metadata { get; set; }
    }
    public enum UploadState
    {
        Created,
        Progress,
        Failed,
        Cancelled,
        Completed,
    }

    public class Progress
    {
        public Progress(long? totalSize, long? uploadedSize, double? completedPercentage, bool isCompleted)
        {
            TotalSize = totalSize;
            UploadedSize = uploadedSize;
            CompletedPercentage = completedPercentage;
            IsCompleted = isCompleted;
        }
        public long? TotalSize { get; set; }
        public long? UploadedSize { get; set; }
        public double? CompletedPercentage { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class PartialSuccessResponse<TKey, TResult, TFailure>
    {
        [JsonConstructor]
        private PartialSuccessResponse(IReadOnlyDictionary<TKey, TResult> succeeded, IReadOnlyDictionary<TKey, TFailure> failures)
        {
            Succeeded = succeeded;
            Failures = failures;
        }
        public IReadOnlyDictionary<TKey, TResult> Succeeded { get; }
        public IReadOnlyDictionary<TKey, TFailure> Failures { get; }

        public PartialSuccessResponse<TKey, TResult, TFailure> AddSuccess(TKey key, TResult result)
            => new PartialSuccessResponse<TKey, TResult, TFailure>(ImmutableDictionary.CreateRange(Succeeded).Add(key, result), Failures);

        public PartialSuccessResponse<TKey, TResult, TFailure> AddFailure(TKey key, TFailure failure)
            => new PartialSuccessResponse<TKey, TResult, TFailure>(Succeeded, ImmutableDictionary.CreateRange(Failures).Add(key, failure));

        public static PartialSuccessResponse<TKey, TResult, TFailure> Empty => new PartialSuccessResponse<TKey, TResult, TFailure>(ImmutableDictionary<TKey, TResult>.Empty,
            ImmutableDictionary<TKey, TFailure>.Empty);

        public PartialSuccessResponse<TKey, TOtherResult, TFailure> Map<TOtherResult>(
            Func<TResult, TOtherResult> mapSucceeded)
            => new PartialSuccessResponse<TKey, TOtherResult, TFailure>(
                Succeeded.Select(pair => new KeyValuePair<TKey, TOtherResult>(pair.Key, mapSucceeded(pair.Value))).ToImmutableDictionary(),
                Failures);
        public PartialSuccessResponse<TKey, TResult, TFailure> MapKeys(
            Func<TKey, TKey> mapKey)
            => new PartialSuccessResponse<TKey, TResult, TFailure>(
                Succeeded.Select(pair => new KeyValuePair<TKey, TResult>(mapKey(pair.Key), pair.Value)).ToImmutableDictionary(),
                Failures.Select(pair => new KeyValuePair<TKey, TFailure>(mapKey(pair.Key), pair.Value)).ToImmutableDictionary());
    }
}