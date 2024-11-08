/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public partial class ServerPermissions
    {
        // CONTENT MODERATION / FILTERING
        // null allowed, and the user's filter could yield an inexact match, second only
        // to an exact one, like....
        //
        //  Setting     User        Priority
        //  false       false           1
        //  false       true            --
        //  false       null            1 (because the user says 'don't care')
        //  true        false           --
        //  true        true            1
        //  true        null            1 (because the user says 'don't care')
        //  null        false           2
        //  null        true            2
        //  null        null            2 (see below)
        //
        // as a side effect, server adminitrators get their servers a better ranking if they
        // put down a definite answer, in opposite being wishy-washy.
        //
        // ref. https://www.techdirt.com/2023/04/20/bluesky-plans-decentralized-composable-moderation/
        //      Defaults to Bluesky in the aforementioned website, with modifications
        //
        // OMITTED
        //
        // (Political) Hate Groups - FALSE - Conflicts the law in many occasions
        // (eg. Germany, §1 GG, §130 StGB)
        //
        // Spam - FALSE - Self-explanatory
        //
        // Impersonation - FALSE - Self-explanatory



        [ProtoMember(1)]
        public bool? Flying = false;

        // Other Nudity (eg. non-sexual or artistic)
        [ProtoMember(2)]
        public bool? Nudity = null;

        // Sexually suggestive (does not include nudity)
        [ProtoMember(3)]
        public bool? Suggestive = null;

        // Violence (Cartoon / "Clean" violence)
        [ProtoMember(4)]
        public bool? Violence = null;

        // Explicit Sexual Images
        [ProtoMember(5)]
        public bool? ExplicitNudes = false;

        // NEW
        //
        // Excessive Violence / Blood (Gore, self-harm, torture)
        [ProtoMember(6)]
        public bool? ExcessiveViolence = false;
    }
}