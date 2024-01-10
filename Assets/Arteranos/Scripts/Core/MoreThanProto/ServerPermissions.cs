/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;

namespace Arteranos.Core
{
    public partial class ServerPermissions : IEquatable<ServerPermissions>
    {
        public (int, int) MatchRatio(ServerPermissions user)
        {
            static int possibleScore(bool? b1) => b1 == null ? 2 : 5;

            int index = 0;
            int possible = 10;

            bool usesGuest = SettingsManager.Client?.Me.Login.IsGuest ?? true;

            bool usesCustomAvatar = SettingsManager.Client?.Me.CurrentAvatar.IsCustom ?? true;

            // The 'Big Three' are true booleans - either true or false, no inbetweens.

            // Double weight for one of the 'Big Three'
            index += Flying.FuzzyEq(user.Flying) * 2;


            // Aggregate the matches of the permission settings against the user's
            // filter settings.
            possible += possibleScore(Nudity);
            index += Nudity.FuzzyEq(user.Nudity);

            possible += possibleScore(Suggestive);
            index += Suggestive.FuzzyEq(user.Suggestive);

            possible += possibleScore(Violence);
            index += Violence.FuzzyEq(user.Violence);

            possible += possibleScore(ExcessiveViolence);
            index += ExcessiveViolence.FuzzyEq(user.ExcessiveViolence);

            possible += possibleScore(ExplicitNudes);
            index += ExplicitNudes.FuzzyEq(user.ExplicitNudes);

            return (index, possible);
        }

        public string HumanReadableMI(ServerPermissions user)
        {
            (int index, int possible) = MatchRatio(user);
            float ratio = (float) index / (float) possible;

            string str = ratio switch
            {
               >= 1.0f => "perfect",
                > 0.8f => "very good",
                > 0.6f => "good",
                > 0.4f => "mediocre",
                > 0.2f => "poor",
                     _ => "very poor"
            };

            return $"{index} ({str})";
        }

        public bool IsInViolation(ServerPermissions serverPerms)
        {
            int points = 0;

            static int penalty(bool world, bool? server, int unclear = 1, int clear = 5)
            {
                // The world has the warning label unset, so it's okay.
                if (!world) return 0;

                // And, the world's warning label _is_ set, so....
                // The server's permissions are unclear.
                if (server == null) return unclear;

                // The server okays the warning label.
                if (server.Value) return 0;

                // The world is in clear violation of the server's permissions.
                return clear;
            }

            points += penalty(Violence ?? true, serverPerms.Violence);
            points += penalty(Nudity ?? true, serverPerms.Nudity);
            points += penalty(Suggestive ?? true, serverPerms.Suggestive);
            points += penalty(ExcessiveViolence ?? true, serverPerms.ExcessiveViolence, 2);
            points += penalty(ExplicitNudes ?? true, serverPerms.ExplicitNudes, 2);

            return points > 2;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerPermissions);
        }

        public bool Equals(ServerPermissions other)
        {
            return other is not null &&
                   Flying == other.Flying &&
                   ExplicitNudes == other.ExplicitNudes &&
                   Nudity == other.Nudity &&
                   Suggestive == other.Suggestive &&
                   Violence == other.Violence &&
                   ExcessiveViolence == other.ExcessiveViolence;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Flying, ExplicitNudes, Nudity, Suggestive, Violence, ExcessiveViolence);
        }

        public static bool operator ==(ServerPermissions left, ServerPermissions right)
        {
            return EqualityComparer<ServerPermissions>.Default.Equals(left, right);
        }

        public static bool operator !=(ServerPermissions left, ServerPermissions right)
        {
            return !(left == right);
        }
    }
}