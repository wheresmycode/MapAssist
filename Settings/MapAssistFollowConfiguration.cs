/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using MapAssist.Files;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using System.Collections.Generic;
using System.Drawing;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{

    public class FollowConfiguration
    {
        [YamlMember(Alias = "Leader", ApplyNamingConventions = false)]
        public string Leader { get; set; }

        [YamlMember(Alias = "FollowRange", ApplyNamingConventions = false)]
        public int FollowRange { get; set; }



        [YamlMember(Alias = "InputDelay", ApplyNamingConventions = false)]
        public int InputDelay { get; set; }

        [YamlMember(Alias = "HealthPot", ApplyNamingConventions = false)]
        public int HealthPot { get; set; }

        [YamlMember(Alias = "HealthRejuv", ApplyNamingConventions = false)]
        public int HealthRejuv { get; set; }

        [YamlMember(Alias = "ManaPot", ApplyNamingConventions = false)]
        public int ManaPot { get; set; }

        [YamlMember(Alias = "ManaRejuv", ApplyNamingConventions = false)]
        public int ManaRejuv { get; set; }

        [YamlMember(Alias = "AttackRange", ApplyNamingConventions = false)]
        public int AttackRange { get; set; }

        [YamlMember(Alias = "IgnoreMonsters", ApplyNamingConventions = false)]
        public List<string> IgnoreMonsters { get; set; }

        [YamlMember(Alias = "IgnoreImmunities", ApplyNamingConventions = false)]
        public List<Resist> IgnoreImmunities { get; set; } = new List<Resist> { };

        [YamlMember(Alias = "CastOnLeader", ApplyNamingConventions = false)]
        public bool CastOnLeader { get; set; }

        [YamlMember(Alias = "UseRightSkills", ApplyNamingConventions = false)]
        public string[] UseRightSkills { get; set; }

        [YamlMember(Alias = "UseLeftSkills", ApplyNamingConventions = false)]
        public string[] UseLeftSkills { get; set; }

        [YamlMember(Alias = "CastSequenceRight", ApplyNamingConventions = false)]
        public int[] CastSequenceRight { get; set; }

        [YamlMember(Alias = "CastSequenceLeft", ApplyNamingConventions = false)]
        public int[] CastSequenceLeft { get; set; }

        [YamlMember(Alias = "CastCooldownRight", ApplyNamingConventions = false)]
        public uint CastCooldownRight { get; set; }

        [YamlMember(Alias = "CastCooldownLeft", ApplyNamingConventions = false)]
        public uint CastCooldownLeft { get; set; }
    }

}
