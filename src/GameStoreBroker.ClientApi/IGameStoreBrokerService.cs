﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(IAccessTokenProvider accessTokenProvider, string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(IAccessTokenProvider accessTokenProvider, string productId, CancellationToken ct);
    }
}