/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Threading.Tasks;

namespace Arteranos.Web
{
    public interface IConnectionManager
    {
        Task<bool> ConnectToServer(string serverURL);
    }

}
