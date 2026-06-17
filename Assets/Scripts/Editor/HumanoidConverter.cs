using UnityEngine;
using UnityEditor;

public class HumanoidConverter : EditorWindow
{
    // Đường dẫn thư mục mặc định theo yêu cầu của bạn
    private string folderPath = "Assets/WizardAnimation";

    [MenuItem("Tools/Chuyển Animation sang Humanoid")]
    public static void ShowWindow()
    {
        GetWindow<HumanoidConverter>("Chuyển sang Humanoid");
    }

    private void OnGUI()
    {
        GUILayout.Label("Thiết lập thư mục cần xử lý", EditorStyles.boldLabel);

        // Cho phép bạn sửa đường dẫn nếu sau này muốn áp dụng cho thư mục khác
        folderPath = EditorGUILayout.TextField("Đường dẫn (Path):", folderPath);

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Công cụ này sẽ tìm tất cả các file FBX/Model trong thư mục được chỉ định và tự động chuyển Animation Type của chúng sang Humanoid.", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Bắt đầu Chuyển đổi", GUILayout.Height(40)))
        {
            ConvertAnimationsToHumanoid();
        }
    }

    private void ConvertAnimationsToHumanoid()
    {
        // Kiểm tra xem thư mục có tồn tại không
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"<color=red>Lỗi:</color> Không tìm thấy thư mục '{folderPath}'. Vui lòng kiểm tra lại đường dẫn!");
            return;
        }

        // Tìm tất cả các file Model (FBX, OBJ...) trong thư mục được chỉ định
        string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { folderPath });

        int totalModels = guids.Length;
        int changedCount = 0;

        if (totalModels == 0)
        {
            Debug.Log($"Không tìm thấy file animation/model nào trong thư mục {folderPath}.");
            return;
        }

        try
        {
            for (int i = 0; i < totalModels; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Hiển thị thanh tiến trình
                EditorUtility.DisplayProgressBar("Đang xử lý Animation", $"Đang kiểm tra: {path}", (float)i / totalModels);

                // Lấy ModelImporter của file
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                // Nếu lấy thành công và Type hiện tại CHƯA PHẢI là Humanoid
                if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
                {
                    // 1. Chuyển Type sang Humanoid
                    importer.animationType = ModelImporterAnimationType.Human;

                    // 2. Thiết lập Avatar Definition là "Create From This Model" (Tạo Avatar tự động)
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                    // Lưu cài đặt và bắt Unity load lại file
                    importer.SaveAndReimport();

                    changedCount++;
                }
            }
        }
        finally
        {
            // Luôn luôn tắt thanh tiến trình khi chạy xong hoặc có lỗi
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"<color=green>Hoàn tất!</color> Đã kiểm tra {totalModels} file. Tự động chuyển đổi thành công {changedCount} file sang Humanoid.");
    }
}