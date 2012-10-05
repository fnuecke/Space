/*  
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using Engine.FarMath;

namespace ProjectMercury.EffectEditor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using ProjectMercury.EffectEditor.IO;
    using ProjectMercury.EffectEditor.PluginInterfaces;
    using ProjectMercury.Emitters;
    using ProjectMercury.Modifiers;
    using ProjectMercury.Renderers;

    internal class Core : ApplicationContext
    {
        private CompositionContainer CompositionContainer { get; set; }

        private IEnumerable<IEmitterPlugin> _emitterPlugins;

        /// <summary>
        /// Gets or sets the copy plugins.
        /// </summary>
        /// <value>The copy plugins.</value>
        [ImportMany(typeof(IEmitterPlugin), AllowRecomposition = true)]
        public IEnumerable<IEmitterPlugin> EmitterPlugins
        {
            get
            {
                return this._emitterPlugins;
            }
            private set
            {
                this._emitterPlugins = value;

                if (this.EmitterPlugins != null)
                {
                    if (this.EmitterPlugins.Count() == 0)
                        throw new Exception("Could not find any emitter plugins!");

                    Trace.WriteLine("Found " + this.EmitterPlugins.Count() + " emitter plugins...", "CORE");

                    using (new TraceIndenter())
                    {
                        this.EmitterPlugins.ToList().ForEach(p => Trace.WriteLine("Plugin: " + p.Name));
                    }
                }
            }
        }

        private IEnumerable<IModifierPlugin> _modifierPlugins;

        /// <summary>
        /// Gets or sets the modifier plugins.
        /// </summary>
        /// <value>The modifier plugins.</value>
        [ImportMany(typeof(IModifierPlugin), AllowRecomposition = true)]
        public IEnumerable<IModifierPlugin> ModifierPlugins
        {
            get
            {
                return this._modifierPlugins;
            }
            set
            {
                this._modifierPlugins = value;

                if (this.ModifierPlugins != null)
                {
                    if (this.ModifierPlugins.Count() == 0)
                        throw new Exception("Could not find any modifier plugins!");

                    Trace.WriteLine("Found " + this.ModifierPlugins.Count() + " modifier plugins...", "CORE");

                    using (new TraceIndenter())
                    {
                        this.ModifierPlugins.ToList().ForEach(p => Trace.WriteLine("Plugin: " + p.Name));
                    }
                }
            }
        }

        private IEnumerable<IEffectSerializationPlugin> _serializationPlugins;

        /// <summary>
        /// Gets or sets the serialization plugins.
        /// </summary>
        /// <value>The serialization plugins.</value>
        [ImportMany(typeof(IEffectSerializationPlugin), AllowRecomposition = true)]
        public IEnumerable<IEffectSerializationPlugin> SerializationPlugins
        {
            get
            {
                return this._serializationPlugins;
            }
            set
            {
                this._serializationPlugins = value;

                if (this.SerializationPlugins != null)
                {
                    if (this.SerializationPlugins.Count() == 0)
                        throw new Exception("Could not find any serialization plugins!");

                    Trace.WriteLine("Found " + this.SerializationPlugins.Count() + " serialization plugins...", "CORE");

                    using (new TraceIndenter())
                    {
                        this.SerializationPlugins.ToList().ForEach(p => Trace.WriteLine("Plugin: " + p.Name));
                    }
                }
            }
        }

        private IInterfaceProvider _interface;

        /// <summary>
        /// Gets or sets the user interface.
        /// </summary>
        /// <value>The user interface.</value>
        [Import(typeof(IInterfaceProvider))]
        private IInterfaceProvider Interface
        {
            get { return this._interface; }
            set
            {
                this._interface = value;

                if (this.Interface != null)
                    Trace.WriteLine("InterfaceProvider loaded: " + value.GetType().AssemblyQualifiedName, "CORE");
            }
        }

        /// <summary>
        /// Gets or sets the timer object which measures time between app idle events.
        /// </summary>
        private Stopwatch TickTimer { get; set; }

        /// <summary>
        /// Gets or sets the asynchronous update manager.
        /// </summary>
        private AsyncUpdateManager UpdateManager { get; set; }

        /// <summary>
        /// Gets or sets the particle effect renderer.
        /// </summary>
        /// <value>The particle effect renderer.</value>
        private Renderer ParticleEffectRenderer { get; set; }

        /// <summary>
        /// Gets or sets the default texture to use when rendering Particles.
        /// </summary>
        private Texture2D DefaultParticleTexture { get; set; }

        /// <summary>
        /// Gets or sets the ParticleEffect which is being designed.
        /// </summary>
        private ParticleEffect ParticleEffect { get; set; }

        /// <summary>
        /// Gets or sets the list of TextureReferences.
        /// </summary>
        private List<TextureReference> TextureReferences { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Core"/> class.
        /// </summary>
        public Core() : base()
        {
            Trace.WriteLine("Program core initialising...", "CORE");

            this.Compose();

            if (this.Interface is Form)
                base.MainForm = this.Interface as Form;

            this.Interface.Ready += new EventHandler(this.Interface_Ready);
            this.Interface.Serialize += new SerializeEventHandler(this.Interface_Serialize);
            this.Interface.Deserialize += new SerializeEventHandler(this.Interface_Deserialize);
            this.Interface.EmitterAdded +=new NewEmitterEventHandler(this.Interface_EmitterAdded);
            this.Interface.EmitterCloned += new CloneEmitterEventHandler(this.Interface_EmitterCloned);
            this.Interface.EmitterRemoved += new EmitterEventHandler(this.Interface_EmitterRemoved);
            this.Interface.ModifierAdded += new NewModifierEventHandler(this.Interface_ModifierAdded);
            this.Interface.ModifierCloned += new CloneModifierEventHandler(this.Interface_ModifierCloned);
            this.Interface.ModifierRemoved += new ModifierEventHandler(this.Interface_ModifierRemoved);
            this.Interface.EmitterReinitialised += new EmitterReinitialisedEventHandler(this.Interface_EmitterReinitialised);
            this.Interface.TextureReferenceAdded += new NewTextureReferenceEventHandler(this.Interface_TextureReferenceAdded);
            this.Interface.TextureReferenceChanged += new TextureReferenceChangedEventHandler(this.Interface_TextureReferenceChanged);
            this.Interface.NewParticleEffect += new EventHandler(this.Interface_NewParticleEffect);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.ApplicationContext"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Trace.WriteLine("Disposing program core...", "CORE");

                if (this.ParticleEffectRenderer != null)
                    this.ParticleEffectRenderer.Dispose();

                if (this.Interface != null)
                    this.Interface.Dispose();

                if (this.CompositionContainer != null)
                    this.CompositionContainer.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Composes this instance.
        /// </summary>
        private void Compose()
        {
            Trace.WriteLine("Composing IOC container...", "CORE");

            try
            {
                using (var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly()))
                {
                    DirectoryInfo pluginsDirectory = new DirectoryInfo(Path.Combine(Application.StartupPath, "Plugins"));

                    // Ensure that the plugins directory is not blocked, otherwise the plugins will not load...
                    pluginsDirectory.Unblock(true);

                    using (var pluginsCatalog = new DirectoryCatalog(pluginsDirectory.FullName))
                    {
                        Trace.WriteLine("Found plugin assemblies...", "CORE");

                        using (new TraceIndenter())
                        {
                            pluginsCatalog.LoadedFiles.ToList().ForEach(f => Trace.WriteLine("Assembly: " + f));
                        }

                        Trace.WriteLine("Found exported parts...", "CORE");

                        using (new TraceIndenter())
                        {
                            foreach(var part in pluginsCatalog.Parts)
                            {
                                Trace.WriteLine("Part:" + part);

                                using (new TraceIndenter())
                                {
                                    part.ExportDefinitions.ToList().ForEach(e => Trace.WriteLine("Implementing: " + e.ContractName));
                                }
                            }
                        }

                        using (var catalog = new AggregateCatalog(assemblyCatalog, pluginsCatalog))
                        {
                            this.CompositionContainer = new CompositionContainer(catalog);

                            var batch = new CompositionBatch();

                            batch.AddPart(this);

                            this.CompositionContainer.Compose(batch);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);

                throw;
            }
        }

        /// <summary>
        /// Executed when the interface provider is ready.
        /// </summary>
        private void Interface_Ready(object sender, EventArgs e)
        {
            Trace.WriteLine("User interface ready...", "CORE");

            this.ParticleEffectRenderer     = this.InstantiateRenderer();
            this.DefaultParticleTexture     = this.LoadDefaultParticleTexture();
            this.UpdateManager              = new AsyncUpdateManager();
            this.ParticleEffect             = this.InstantiateDefaultParticleEffect();
            this.TextureReferences          = this.LoadDefaultTextureReferences();

            this.Interface.SetEmitterPlugins(this.EmitterPlugins.OrderBy(p => p.DisplayName));
            this.Interface.SetModifierPlugins(this.ModifierPlugins.OrderBy(p => p.DisplayName));
            this.Interface.SetSerializationPlugins(this.SerializationPlugins.OrderBy(p => p.DisplayName));

            this.Interface.TextureReferences = this.TextureReferences;
            this.Interface.SetParticleEffect(this.ParticleEffect);

            this.UpdateManager.Start();

            Application.Idle += new EventHandler(this.Tick);
        }

        /// <summary>
        /// Instantiates the renderer.
        /// </summary>
        private Renderer InstantiateRenderer()
        {
            Trace.WriteLine("Instantiating particle renderer...", "CORE");

            Renderer renderer = new SpriteBatchRenderer
            {
                GraphicsDeviceService = GraphicsDeviceService.Instance
            };

            renderer.LoadContent(null);

            return renderer;
        }

        /// <summary>
        /// Loads the default particle texture.
        /// </summary>
        private Texture2D LoadDefaultParticleTexture()
        {
            Trace.WriteLine("Loading default particle texture...", "CORE");

            using (FileStream inputStream = File.OpenRead("Textures\\FlowerBurst.png"))
            {
                return Texture2D.FromStream(GraphicsDeviceService.Instance.GraphicsDevice, inputStream);
            }
        }

        /// <summary>
        /// Instantiates the default particle effect.
        /// </summary>
        private ParticleEffect InstantiateDefaultParticleEffect()
        {
            Trace.WriteLine("Instantiating default particle effect...", "CORE");

            ParticleEffect effect = new ParticleEffect
            {
                new Emitter
                {
                    Budget                   = 5000,
                    Enabled                  = true,
                    MinimumTriggerPeriod     = 0f,
                    Name                     = "Basic Emitter",
                    ParticleTexture          = this.DefaultParticleTexture,
                    ParticleTextureAssetName = "FlowerBurst",
                    ReleaseColour            = Color.White.ToVector3(),
                    ReleaseOpacity           = 1f,
                    ReleaseQuantity          = 10,
                    ReleaseScale             = new VariableFloat { Value = 32f, Variation = 16f },
                    ReleaseSpeed             = new VariableFloat { Value = 25f, Variation = 25f },
                    Term                     = 1f,
                    Modifiers                = new ModifierCollection(),
                },
            };

            effect.Initialise();

            return effect;
        }

        /// <summary>
        /// Loads the default texture references.
        /// </summary>
        private List<TextureReference> LoadDefaultTextureReferences()
        {
            Trace.WriteLine("Loading default texture references...", "CORE");

            List<TextureReference> references = new List<TextureReference>();

            //DirectoryInfo texturesDirectory = new DirectoryInfo("Textures");
            DirectoryInfo texturesDirectory = new DirectoryInfo(Application.StartupPath + "\\..\\..\\..\\SpaceContentTextures\\Effects");

            if (texturesDirectory.Exists)
            {
                foreach (FileInfo file in texturesDirectory.GetFiles())
                {
                    if (file.Extension == ".bmp" || file.Extension == ".jpg" || file.Extension == ".png")
                    {
                        try
                        {
                            TextureReference reference = new TextureReference(file.FullName);

                            references.Add(reference);

                            using (new TraceIndenter())
                            {
                                Trace.WriteLine("File: " + file.FullName);
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.Message);

                            continue;
                        }
                    }
                }
            }

            return references;
        }

        /// <summary>
        /// Handles the Application Idle event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Tick(object sender, EventArgs e)
        {
            float elapsedSeconds = this.TickTimer != null ? (float)this.TickTimer.Elapsed.TotalSeconds : 1f / 60f;

            if (this.TickTimer == null)
                this.TickTimer = new Stopwatch();

            float x, y;

            if (this.Interface.TriggerRequired(out x, out y, this.ParticleEffect))
                this.ParticleEffect.Trigger(new FarPosition { X = x, Y = y });

            Stopwatch updateTimer = Stopwatch.StartNew();

            this.UpdateManager.BeginUpdate(elapsedSeconds, this.ParticleEffect);
            this.UpdateManager.EndUpdate();

            this.Interface.SetUpdateTime((float)updateTimer.Elapsed.TotalSeconds);

            this.Interface.Draw(this.ParticleEffect, this.ParticleEffectRenderer);

            this.TickTimer.Reset();
            this.TickTimer.Start();
        }

        /// <summary>
        /// Handles the Serialize event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.SerializeEventArgs"/> instance containing the event data.</param>
        private void Interface_Serialize(object sender, SerializeEventArgs e)
        {
            Trace.WriteLine("Serializing particle effect to " + e.FilePath, "CORE");
            
            using (new TraceIndenter())
            {
                Trace.WriteLine("Using plugin: " + e.Plugin.Name);
            }

            try
            {
                e.Plugin.Serialize(this.ParticleEffect, e.FilePath);

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the Deserialize event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.SerializeEventArgs"/> instance containing the event data.</param>
        private void Interface_Deserialize(object sender, SerializeEventArgs e)
        {
            Trace.WriteLine("Deserializing particle effect from " + e.FilePath, "CORE");
            
            using (new TraceIndenter())
            {
                Trace.WriteLine("Using plugin: " + e.Plugin.Name);
            }

            try
            {
                this.ParticleEffect = e.Plugin.Deserialize(e.FilePath);

                this.ParticleEffect.Initialise();

                foreach (Emitter emitter in this.ParticleEffect)
                {
                    if (String.IsNullOrEmpty(emitter.ParticleTextureAssetName))
                    {
                        emitter.ParticleTexture = this.DefaultParticleTexture;
                    }
                    else
                    {
                        bool textureFound = false;

                        foreach (TextureReference reference in this.TextureReferences)
                        {
                            if ("Textures\\Effects\\" + reference.GetAssetName() == emitter.ParticleTextureAssetName.Replace('/', '\\'))
                            {
                                emitter.ParticleTexture = reference.Texture;

                                textureFound = true;

                                break;
                            }
                        }

                        if (!textureFound)
                        {
                            MessageBox.Show("Could not find texture asset '" + emitter.ParticleTextureAssetName + "'. " +
                                "Using default particle texture...", "Asset not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                            emitter.ParticleTexture = this.DefaultParticleTexture;
                        }
                    }
                }

                this.Interface.SetParticleEffect(this.ParticleEffect);

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the EmitterAdded event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.NewEmitterEventArgs"/> instance containing the event data.</param>
        public void Interface_EmitterAdded(object sender, NewEmitterEventArgs e)
        {
            Trace.WriteLine("Adding emitter...", "CORE");

            using (new TraceIndenter())
            {
                Trace.WriteLine("Using plugin: " + e.Plugin.Name);
            }

            try
            {
                Emitter emitter = e.Plugin.CreateDefaultInstance();

                emitter.Initialise(e.Budget, e.Term);

                emitter.ParticleTexture = this.DefaultParticleTexture;

                this.ParticleEffect.Add(emitter);

                e.AddedEmitter = emitter;

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the EmitterCloned event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.CloneEmitterEventArgs"/> instance containing the event data.</param>
        public void Interface_EmitterCloned(object sender, CloneEmitterEventArgs e)
        {
            Trace.WriteLine("Cloning emitter...", "CORE");

            try
            {
                Emitter clone = e.Prototype.DeepCopy();

                clone.Initialise();

                clone.ParticleTexture = e.Prototype.ParticleTexture;

                this.ParticleEffect.Add(clone);

                e.AddedEmitter = clone;

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the ModifierAdded event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.NewModifierEventArgs"/> instance containing the event data.</param>
        public void Interface_ModifierAdded(object sender, NewModifierEventArgs e)
        {
            Trace.WriteLine("Adding modifier...", "CORE");

            using (new TraceIndenter())
            {
                Trace.WriteLine("Using plugin: " + e.Plugin.Name);
            }

            try
            {
                foreach (Emitter emitter in this.ParticleEffect)
                {
                    if (Object.ReferenceEquals(emitter, e.ParentEmitter))
                    {
                        Modifier modifier = e.Plugin.CreateDefaultInstance();

                        emitter.Modifiers.Add(modifier);

                        e.AddedModifier = modifier;

                        e.Result = CoreOperationResult.OK;

                        return;
                    }
                }

                e.Result = new CoreOperationResult(new Exception("Could not find the specified Emitter."));
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the ModifierCloned event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.CloneModifierEventArgs"/> instance containing the event data.</param>
        public void Interface_ModifierCloned(object sender, CloneModifierEventArgs e)
        {
            Trace.WriteLine("Cloning emitter...", "CORE");

            try
            {
                Modifier clone = e.Prototype.DeepCopy();

                foreach (Emitter emitter in this.ParticleEffect)
                {
                    foreach (Modifier modifier in emitter.Modifiers)
                    {
                        if (Object.ReferenceEquals(modifier, e.Prototype))
                        {
                            emitter.Modifiers.Add(clone);

                            e.AddedModifier = clone;

                            e.Result = CoreOperationResult.OK;

                            return;
                        }
                    }
                }

                e.Result = new CoreOperationResult(new Exception("Could not find modifier prototype in effect hierarchy."));
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the EmitterRemoved event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.EmitterEventArgs"/> instance containing the event data.</param>
        public void Interface_EmitterRemoved(object sender, EmitterEventArgs e)
        {
            Trace.WriteLine("Removing emitter...", "CORE");

            try
            {
                foreach (Emitter emitter in this.ParticleEffect)
                {
                    if (Object.ReferenceEquals(emitter, e.Emitter))
                    {
                        this.ParticleEffect.Remove(e.Emitter);

                        e.Result = CoreOperationResult.OK;

                        return;
                    }
                }

                e.Result = new CoreOperationResult(new Exception("Could not find the specified Emitter."));
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the ModifierRemoved event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.ModifierEventArgs"/> instance containing the event data.</param>
        public void Interface_ModifierRemoved(object sender, ModifierEventArgs e)
        {
            Trace.WriteLine("Removing modifier...", "CORE");

            try
            {
                foreach (Emitter emitter in this.ParticleEffect)
                {
                    foreach (Modifier modifier in emitter.Modifiers)
                    {
                        if (Object.ReferenceEquals(modifier, e.Modifier))
                        {
                            emitter.Modifiers.Remove(e.Modifier);

                            e.Result = CoreOperationResult.OK;

                            return;
                        }
                    }
                }

                e.Result = new CoreOperationResult(new Exception("Could not find the specified Modifier."));
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the EmitterReinitialised event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.EmitterReinitialisedEventArgs"/> instance containing the event data.</param>
        public void Interface_EmitterReinitialised(object sender, EmitterReinitialisedEventArgs e)
        {
            Trace.WriteLine("Reinitialising emitter...", "CORE");

            try
            {
                e.Emitter.Initialise(e.Budget, e.Term);

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the TextureReferenceAdded event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.NewTextureReferenceEventArgs"/> instance containing the event data.</param>
        private void Interface_TextureReferenceAdded(object sender, NewTextureReferenceEventArgs e)
        {
            Trace.WriteLine("Adding new texture reference...", "CORE");

            using (new TraceIndenter())
            {
                Trace.WriteLine("Filepath: " + e.FilePath);
            }

            try
            {
                TextureReference reference = new TextureReference(e.FilePath);

                e.AddedTextureReference = reference;

                this.TextureReferences.Add(reference);

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the TextureReferenceChanged event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectMercury.EffectEditor.TextureReferenceChangedEventArgs"/> instance containing the event data.</param>
        private void Interface_TextureReferenceChanged(object sender, TextureReferenceChangedEventArgs e)
        {
            try
            {
                e.Emitter.ParticleTexture = e.TextureReference.Texture;

                e.Emitter.ParticleTextureAssetName = e.TextureReference.GetAssetName();

                e.Result = CoreOperationResult.OK;
            }
            catch (Exception error)
            {
                e.Result = new CoreOperationResult(error);
            }
        }

        /// <summary>
        /// Handles the NewParticleEffect event of the Interface.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Interface_NewParticleEffect(object sender, EventArgs e)
        {
            this.ParticleEffect = this.InstantiateDefaultParticleEffect();

            this.Interface.SetParticleEffect(this.ParticleEffect);
        }
    }
}