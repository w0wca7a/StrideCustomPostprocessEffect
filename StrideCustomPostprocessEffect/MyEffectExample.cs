using Stride.Core;
using Stride.Core.Annotations;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace StrideCustomPostprocessEffect
{
    [DataContract("MyEffectExample")]
    public class MyEffectExample : ImageEffect
    {
        [DataMember]
        public bool FishEye { get; set; } = true;

        [DataMember]
        [DataMemberRange(0, 6, 0.1, 0.2, 1)]
        public float ScanSpeedAdd { get; set; } = 6.0f;

        [DataMember]
        [DataMemberRange(0, 0.5, 0.1, 0.15, 1)]
        public float ScanlineSize { get; set; } = 0.1f;

        [DataMember]
        [DataMemberRange(0, 1, 0.1, 0.2, 1)]
        public float WhiteIntensity { get; set; } = 0.8f;

        [DataMember]
        [DataMemberRange(0, 1, 0.1, 0.2, 1)]
        public float AnaglyphIntensity { get; set; } = 0.5f;

        private ImageEffectShader OldMonitorScanlines;

        protected override void InitializeCore()
        {
            base.InitializeCore();
            OldMonitorScanlines = ToLoadAndUnload(new ImageEffectShader("OldMonitorScanlines"));
        }
        
        protected override void DrawCore(RenderDrawContext context)
        {
            var GlobalTime = Services.GetService<IGame>().UpdateTime.Elapsed.TotalMilliseconds; 
            //inputs
            Texture colorBuffer = GetSafeInput(0);
            //Texture velocityBuffer = GetSafeInput(1);
            //Texture depthBuffer = GetSafeInput(2);

            //output
            Texture outputBuffer = GetSafeOutput(0);

            OldMonitorScanlines.SetInput(0, colorBuffer);
            OldMonitorScanlines.SetOutput(outputBuffer);
            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.iTime, (float)GlobalTime);

            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.scanSpeedAdd, ScanSpeedAdd);
            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.lineCut, ScanlineSize);
            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.whiteIntensity, WhiteIntensity);
            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.anaglyphIntensity, AnaglyphIntensity);
            OldMonitorScanlines.Parameters.Set(OldMonitorScanlinesKeys.fisheye, FishEye);

            OldMonitorScanlines.Draw(context);
        }
    }
}
