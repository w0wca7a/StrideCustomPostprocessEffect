using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Annotations;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace StrideCustomPostprocessEffect
{
    /// <summary>
    /// A default bundle of <see cref="ImageEffect"/>.
    /// </summary>
    [DataContract("CustomPostProcessingEffects")]
    [Display("Custom Post-processing effects")]
    public sealed class CustomPostProcessingEffects : ImageEffect, IImageEffectRenderer, IPostProcessingEffects
    {        
        public MyEffectExample MyEffect{ get; set; }

        [DataMember(70)]
        [Category]
        public ColorTransformGroup ColorTransforms => colorTransformsGroup;

        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; } = Guid.NewGuid();

        private ColorTransformGroup colorTransformsGroup;
        public CustomPostProcessingEffects(IServiceRegistry services) : this(RenderContext.GetShared(services)) { }
        public CustomPostProcessingEffects()
        {
            colorTransformsGroup = new ColorTransformGroup();
            MyEffect = new MyEffectExample();
        }

        public CustomPostProcessingEffects(RenderContext context) : this() { Initialize(context); } 
        public bool RequiresVelocityBuffer => true;
        public bool RequiresNormalBuffer => true;
        public bool RequiresSpecularRoughnessBuffer => false;
        public void Collect(RenderContext context) { }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            MyEffect = ToLoadAndUnload(MyEffect);   
            colorTransformsGroup = ToLoadAndUnload(colorTransformsGroup);
        }

        public void Draw(RenderDrawContext drawContext, RenderOutputValidator outputValidator, Span<Texture> inputs, Texture inputDepthStencil, Texture outputTarget) 
        {
            var colorIndex = outputValidator.Find<ColorTargetSemantic>();
            if (colorIndex < 0) return;

            SetInput(0, inputs[colorIndex]);
            SetInput(1, inputDepthStencil);

            var normalsIndex = outputValidator.Find<NormalTargetSemantic>();
            if (normalsIndex >= 0)
            {
                SetInput(2, inputs[normalsIndex]);
            }

            var specularRoughnessIndex = outputValidator.Find<SpecularColorRoughnessTargetSemantic>();
            if (specularRoughnessIndex >= 0)
            {
                SetInput(3, inputs[specularRoughnessIndex]);
            }

            var reflectionIndex0 = outputValidator.Find<OctahedronNormalSpecularColorTargetSemantic>();
            var reflectionIndex1 = outputValidator.Find<EnvironmentLightRoughnessTargetSemantic>();
            if (reflectionIndex0 >= 0 && reflectionIndex1 >= 0)
            {
                SetInput(4, inputs[reflectionIndex0]);
                SetInput(5, inputs[reflectionIndex1]);
            }

            var velocityIndex = outputValidator.Find<VelocityTargetSemantic>();
            if (velocityIndex != -1)
            {
                SetInput(6, inputs[velocityIndex]);
            }

            SetOutput(outputTarget);
            Draw(drawContext);
        }

        protected override void DrawCore(RenderDrawContext context) 
        {
            var input = GetInput(0);
            var output = GetOutput(0);
            if (input == null || output == null)  return;

            var inputDepthTexture = GetInput(1); // Depth

            // Update the parameters for this post effect
            if (!Enabled)
            {
                if (input != output)
                {
                    Scaler.SetInput(input);
                    Scaler.SetOutput(output);
                    Scaler.Draw(context);
                }
                return;
            }

            // If input == output, than copy the input to a temporary texture
            if (input == output)
            {
                var newInput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                context.CommandList.Copy(input, newInput);
                input = newInput;
            }

            var currentInput = input;

            if (MyEffect.Enabled)
            {
                // blurred output
                var myEffectOut = NewScopedRenderTarget2D(currentInput.Width, currentInput.Height, currentInput.Format);
                // color input
                MyEffect.SetInput(0, currentInput);

                // velocity input
                MyEffect.SetInput(1, GetInput(6));

                // depth input
                MyEffect.SetInput(2, inputDepthTexture);

                MyEffect.SetOutput(myEffectOut);                
                MyEffect.Draw(context);
                currentInput = myEffectOut;
            }

            var toneOutput =  output;

            // Color transform group pass (tonemap, color grading)
            var lastEffect = colorTransformsGroup.Enabled ? (ImageEffect)colorTransformsGroup : Scaler;
            lastEffect.SetInput(currentInput);
            lastEffect.SetOutput(toneOutput);
            lastEffect.Draw(context);
        }

        public void DisableAll()
        {
            MyEffect.Enabled = false;
            colorTransformsGroup.Enabled = false;
        }
    }
}
