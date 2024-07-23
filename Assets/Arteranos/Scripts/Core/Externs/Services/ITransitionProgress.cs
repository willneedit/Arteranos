/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.Services
{
    public interface ITransitionProgress
    {
        void OnProgressChanged(float progress, string progressText);
    }
}