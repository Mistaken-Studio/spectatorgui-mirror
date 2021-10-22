// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using HarmonyLib;

namespace Mistaken.SpectatorGUI
{
    /// <inheritdoc/>
    internal class PluginHandler : Plugin<Config, Translation>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "SpectatorGUI";

        /// <inheritdoc/>
        public override string Prefix => "MSGUI";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Default;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(3, 0, 3);

#pragma warning disable SA1202 // Elements should be ordered by access
        private Version version;

        /// <inheritdoc/>
        public override Version Version
        {
            get
            {
                if (this.version == null)
                    this.version = this.Assembly.GetName().Version;
                return this.version;
            }
        }
#pragma warning restore SA1202 // Elements should be ordered by access

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            this.harmony = new Harmony("mistaken.spectatrgui");
            this.harmony.PatchAll();

            new SpecInfoHandler(this);

            API.Diagnostics.Module.OnEnable(this);

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            this.harmony.UnpatchAll();

            API.Diagnostics.Module.OnDisable(this);

            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        private Harmony harmony;
    }
}
