﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessToken();
    }
}