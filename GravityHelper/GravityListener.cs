﻿using System;
using Monocle;

namespace GravityHelper
{
    [Tracked]
    public class GravityListener : Component
    {
        public GravityListener(Action<GravityHelperModule.GravityTypes> onChangeGravity = null)
            : base(true, false)
        {
            OnChangeGravity = onChangeGravity;
        }

        public Action<GravityHelperModule.GravityTypes> OnChangeGravity;
    }
}
