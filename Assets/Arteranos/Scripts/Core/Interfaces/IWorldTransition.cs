/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;

namespace Arteranos.Web
{
    public interface IWorldTransition
    {
        void InitiateTransition(string url, Action failureCallback = null, Action successCallback = null);
    }
}
