﻿using Spectrum.Plugins.ServerMod.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Spectrum.Plugins.ServerMod.PlaylistTools.LevelFilters
{
    class LevelFilterAuthor : LevelFilter
    {
        public override string[] options { get; } = new string[] {"a", "author"};

        string match = "";
        string matchRegex = "";

        public LevelFilterAuthor() { }

        public LevelFilterAuthor(string match)
        {
            this.match = match.ToLower().Trim();
            matchRegex = GeneralUtilities.getSearchRegex(match);
        }

        public override void Apply(List<PlaylistLevel> levels)
        {
            var levelSetsManager = G.Sys.LevelSets_;

            var exactMatch = false;
            foreach (var level in levels)
            {
                var authorName = GeneralUtilities.getAuthorName(levelSetsManager.GetLevelInfo(level.level.levelNameAndPath_.levelPath_));
                if (authorName.ToLower().Trim() == match)
                {
                    exactMatch = true;
                }
            }
            if (exactMatch)
                foreach (var level in levels)
                {
                    var authorName = GeneralUtilities.getAuthorName(levelSetsManager.GetLevelInfo(level.level.levelNameAndPath_.levelPath_));
                    level.Mode(mode, authorName.ToLower().Trim() == match);
                }
            else
                foreach (var level in levels)
                {
                    var authorName = GeneralUtilities.getAuthorName(levelSetsManager.GetLevelInfo(level.level.levelNameAndPath_.levelPath_));
                    level.Mode(mode, Regex.Match(authorName, matchRegex, RegexOptions.IgnoreCase).Success);
                }
        }

        public override LevelFilterResult FromChatString(string chatString, string option)
        {
            return new LevelFilterResult(new LevelFilterAuthor(chatString));
        }
    }
}
