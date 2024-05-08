namespace Kernelmethod.Hagiography {
    public class Options {
        public static bool AutomaticUpload =>
            XRL.UI.Options
                .GetOption("Kernelmethod_Hagiography_AutoUpload")
                .EqualsNoCase("Yes");
    }
}
