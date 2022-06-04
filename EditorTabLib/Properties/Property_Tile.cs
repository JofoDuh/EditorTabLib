﻿using System;
using System.Collections.Generic;

namespace EditorTabLib.Properties
{
    public class Property_Tile : Property
    {
        private readonly List<object> value_default;
        private readonly int min;
        private readonly int max;

        public Property_Tile(string name, Tuple<int, TileRelativeTo> value_default = null, int min = int.MinValue, int max = int.MaxValue, string key = null, bool canBeDisabled = false, bool startEnabled = false, Dictionary<string, string> enableIf = null, Dictionary<string, string> disableIf = null)
            : base(name, key, canBeDisabled, startEnabled, enableIf, disableIf)
        {
            this.value_default = value_default != null ? new List<object> { value_default.Item1, value_default.Item2 } : new List<object>();
            this.min = min;
            this.max = max;
        }

        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>()
                    {
                        { "name", name },
                        { "type", "Tile" },
                        { "default", value_default },
                        { "min", min },
                        { "max", max },
                        { "canBeDisabled", canBeDisabled },
                        { "startEnabled", startEnabled },
                        { "enableIf", enableIf },
                        { "disableIf", disableIf },
                        { "key", key }
                    };
        }
    }
}
