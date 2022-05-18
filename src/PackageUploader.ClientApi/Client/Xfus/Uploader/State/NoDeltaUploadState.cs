﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader.State;

internal class NoDeltaUploadState : XfusUploaderState
{
    internal NoDeltaUploadState(XfusApiController xfusApiController, ILogger logger) : base(xfusApiController, logger)
    {
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, CancellationToken ct)
    {
        var uploadProgress = await InitializeAssetAsync(xfusUploadInfo, uploadFile, false, ct).ConfigureAwait(false);
        _logger.LogInformation($"XFUS Asset Initialized. Will upload {new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");

        await FullUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, false, httpTimeoutMs, ct).ConfigureAwait(false);

        _logger.LogTrace($"Upload complete. Total Uploaded: {new ByteSize(_totalBytesUploaded)}");
        return null;
    }
}
