using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

class Program {
    public static void Main(string[] args) {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1280, 800, "DMG");
        Raylib.SetTargetFPS(60);

        rlImGui.Setup(true);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        //Unsafe access for no imgui.ini files
        var io = ImGui.GetIO();
        unsafe {
            IntPtr ioPtr = (IntPtr)io.NativePtr;
            ImGuiIO* imguiIO = (ImGuiIO*)ioPtr.ToPointer();
            imguiIO->IniFilename = null;
        }

        Image icon = Raylib.LoadImage("icon.png");
        Raylib.SetWindowIcon(icon);

        GUI gui = new GUI();

        gui.Run();
    }
}