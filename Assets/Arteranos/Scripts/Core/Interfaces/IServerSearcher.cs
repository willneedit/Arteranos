/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;

namespace Arteranos.Web
{
    public interface IServerSearcher
    {
        void InitiateServerTransition(string worldURL);
        void InitiateServerTransition(string worldURL, Action<string, string> OnSuccessCallback, Action OnFailureCallback);
    }
}