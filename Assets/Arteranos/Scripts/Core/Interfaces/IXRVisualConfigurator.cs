/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.XR
{
    public interface IXRVisualConfigurator
    {
        void StartFading(float opacity, float duration = 0.5F);
    }
}
