using UnityEngine;
using UnityEditor;

public class TextureSizeReducer : EditorWindow
{
    // Mặc định ép xuống 512 như bạn yêu cầu
    private int targetMaxSize = 512;

    // Tạo menu trên thanh công cụ của Unity
    [MenuItem("Tools/Giảm Size Texture Hàng Loạt")]
    public static void ShowWindow()
    {
        // Mở cửa sổ công cụ
        GetWindow<TextureSizeReducer>("Giảm Size Texture");
    }

    private void OnGUI()
    {
        GUILayout.Label("Thiết lập kích thước Texture", EditorStyles.boldLabel);

        // Cung cấp ô nhập liệu để sau này bạn muốn đổi sang 256 hoặc 1024 thì dễ dàng tùy chỉnh
        targetMaxSize = EditorGUILayout.IntField("Max Size (VD: 512):", targetMaxSize);

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Công cụ này sẽ quét toàn bộ thư mục Assets (giống t:Texture) và giảm Max Size của các ảnh lớn hơn mức thiết lập.", MessageType.Info);
        GUILayout.Space(10);

        // Nút bấm thực thi
        if (GUILayout.Button("Bắt đầu Quét và Giảm Size", GUILayout.Height(40)))
        {
            ReduceTextureSizes();
        }
    }

    private void ReduceTextureSizes()
    {
        // 1. Tìm toàn bộ GUID của các file là Texture (Tương đương việc gõ t:Texture trên ô search)
        // Chỉ giới hạn tìm trong thư mục "Assets" để tránh đụng chạm vào các file của Unity Packages
        string[] guids = AssetDatabase.FindAssets("t:Texture", new string[] { "Assets" });

        int totalTextures = guids.Length;
        int changedCount = 0;

        // Dùng try-finally để đảm bảo thanh tiến trình luôn được tắt đi kể cả khi code bị lỗi giữa chừng
        try
        {
            for (int i = 0; i < totalTextures; i++)
            {
                // Chuyển GUID thành đường dẫn file thực tế (VD: Assets/Textures/myImage.png)
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Hiển thị thanh Loading để Editor không bị đơ
                EditorUtility.DisplayProgressBar("Đang xử lý Textures", $"Đang kiểm tra: {path}", (float)i / totalTextures);

                // Lấy bộ nạp cài đặt của file ảnh đó
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                // Nếu ảnh hợp lệ VÀ Max Size hiện tại đang lớn hơn mức mong muốn (VD: 2048 > 512)
                if (importer != null && importer.maxTextureSize > targetMaxSize)
                {
                    // Ép Max Size xuống
                    importer.maxTextureSize = targetMaxSize;

                    // Lưu lại và bắt Unity re-import file ảnh này
                    importer.SaveAndReimport();

                    changedCount++;
                }
            }
        }
        finally
        {
            // Tắt thanh Loading đi
            EditorUtility.ClearProgressBar();
        }

        // Thông báo kết quả ra Console
        Debug.Log($"<color=green>Hoàn tất!</color> Đã tìm thấy {totalTextures} file ảnh. Trong đó đã giảm size của {changedCount} ảnh xuống {targetMaxSize}.");
    }
}