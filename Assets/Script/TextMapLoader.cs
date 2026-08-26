using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class TextMapLoader : MonoBehaviour
{
    [Header("ーー 使用するアセット（Tile） ーー")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileBase _grassTile;     // 1番上の列（表面）に敷く草タイル
    [SerializeField] private TileBase _dirtTile;      // 2本目以降の列（地中）に敷く土タイル
    [SerializeField] private TileBase _platformTile;  // 🟩 追加：= に対応する空中足場タイル

    [Header("ーー 使用するアセット（Prefab） ーー")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private GameObject _goalPrefab;

    [Header("ーー 読み込むステージのテキストファイル名 ーー")]
    [SerializeField] private string _stageFileName = "Stage1"; 

    void Start()
    {
        _tilemap.ClearAllTiles();
        LoadStageFromText();
    }

    private void LoadStageFromText()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(_stageFileName);
        
        if (textAsset == null)
        {
            Debug.LogError($"Resourcesフォルダー内に {_stageFileName} が見つかりません！");
            return;
        }

        // 🟩 修正1：RemoveEmptyEntries に戻しつつ、末尾の改行ゴミを安全にカットする
        // これにより、Macのテキストエディタ特有の末尾の空行を除外して、純粋な20行のデータとして認識させます
        string[] stageLines = textAsset.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        int mapHeight = stageLines.Length;

        // テキスト全体の「一番長い行の文字数」をあらかじめ割り出し、マップの横幅を固定する
        int maxMapWidth = 0;
        foreach (string line in stageLines)
        {
            if (line.Length > maxMapWidth) maxMapWidth = line.Length;
        }

        for (int y = 0; y < mapHeight; y++)
        {
            string line = stageLines[y];

            for (int x = 0; x < maxMapWidth; x++)
            {
                // もしこの行の文字数が最大幅より短かったら、はみ出さないように空気(.)として扱う安全ガード
                char tileChar = (x < line.Length) ? line[x] : '.';
                
                int worldY = mapHeight - 1 - y; 
                Vector3Int cellPos = new Vector3Int(x, worldY, 0);
                Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

                switch (tileChar)
                {
                    case '#': // 地面の場合
                        bool isSurface = false;
                        
                        // 1マス上が「.（空気）」か「E（敵）」か「P（プレイヤー）」なら、そこは地面の『表面』である
                        if (y > 0)
                        {
                            string upperLine = stageLines[y - 1];
                            if (x < upperLine.Length)
                            {
                                char upperTileChar = upperLine[x];
                                // 💡 修正2：上が「=」の時も、そこは空気扱い（草を置く）にするガードを追加
                                if (upperTileChar == '.' || upperTileChar == 'E' || upperTileChar == 'P' || upperTileChar == '=')
                                {
                                    isSurface = true;
                                }
                            }
                            else
                            {
                                isSurface = true;
                            }
                        }
                        else
                        {
                            isSurface = true; // 天井は表面
                        }

                        if (isSurface)
                        {
                            _tilemap.SetTile(cellPos, _grassTile); // 1番上なら草！
                        }
                        else
                        {
                            _tilemap.SetTile(cellPos, _dirtTile);  // 地中なら土！
                        }
                        break;

                    case '=': // 🟩 追加：新しく設計図に登場した空中足場用のタイル
                        _tilemap.SetTile(cellPos, _platformTile != null ? _platformTile : _grassTile);
                        break;

                    case 'E': // 敵
                        Instantiate(_enemyPrefab, worldPos, Quaternion.identity);
                        break;

                    case 'P': // プレイヤー
                        if (Player.Instance == null)
                        {
                            Instantiate(_playerPrefab, worldPos, Quaternion.identity);
                        }
                        else
                        {
                            Player.Instance.transform.position = worldPos;
                        }
                        break;
                    case 'G': // ゴール
                        Instantiate(_goalPrefab, worldPos, Quaternion.identity);
                        break;

                    default:
                        break;
                }
            }
        }

        Debug.Log($"{_stageFileName} の草と土、新足場(=)を含むすべての完全生成が完了しました！");
    }
}
