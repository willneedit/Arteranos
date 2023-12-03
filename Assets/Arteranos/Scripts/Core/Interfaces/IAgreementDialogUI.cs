/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using System;

namespace Arteranos.UI
{
    public interface IAgreementDialogUI
    {
        Action OnAgree { get; set; }
        Action OnDisagree { get; set; }
        string rtText { get; set; }
        string TextHash { get; set; }

        string MD2RichText(string text);
    }
}
