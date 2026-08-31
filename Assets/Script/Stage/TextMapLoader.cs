using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TextMapLoader : MonoBehaviour
{
    private static readonly Subject<Unit> _mapGenerate = new();
    public static Subject<Unit> MapGenerate => _mapGenerate;

    private static readonly Subject<GameObject> _enemyObj = new Subject<GameObject>();

    public static Subject<GameObject> EnemyObj => _enemyObj;

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

    private readonly CompositeDisposable _disposables = new();

    private string[] _stageLines;
    private int _mapHeight;
    private int _maxMapWidth;

    void Start()
    {
        _tilemap.ClearAllTiles();
        ParseTextData();

        GameManager.PlayerGenerate
        .Subscribe(_ =>
        {
            Debug.Log("プレイヤースポーンを生成");
            GenerateObjectPlayerGenerate();
        }).AddTo(this);

        GameManager.EnemyGenerate
        .Take(1)
        .Subscribe(_ =>
        {
            GenerateObjectsOnly();
        }).AddTo(this);
    }

    private void ParseTextData()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(_stageFileName);
        if (textAsset == null)
        {
            Debug.LogError($"Resourcesフォルダー内に {_stageFileName} が見つかりません！");
            return;
        }

        _stageLines = textAsset.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        _mapHeight = _stageLines.Length;

        _maxMapWidth = 0;
        foreach (string line in _stageLines)
        {
            if (line.Length > _maxMapWidth) _maxMapWidth = line.Length;
        }
        GenerateTilemapOnly();
    }

    private void GenerateTilemapOnly()
    {
        if (_stageLines == null) return;

        for (int y = 0; y < _mapHeight; y++)
        {
            string line = _stageLines[y];
            for (int x = 0; x < _maxMapWidth; x++)
            {
                char tileChar = (x < line.Length) ? line[x] : '.';
                int worldY = _mapHeight - 1 - y; 
                Vector3Int cellPos = new Vector3Int(x, worldY, 0);

                switch (tileChar)
                {
                    case '#': // 地面
                        bool isSurface = false;
                        if (y > 0)
                        {
                            string upperLine = _stageLines[y - 1];
                            if (x < upperLine.Length)
                            {
                                char upperTileChar = upperLine[x];
                                if (upperTileChar == '.' || upperTileChar == 'E' || upperTileChar == 'P' || upperTileChar == '=')
                                {
                                    isSurface = true;
                                }
                            }
                            else { isSurface = true; }
                        }
                        else { isSurface = true; }

                        if (isSurface) _tilemap.SetTile(cellPos, _grassTile);
                        else _tilemap.SetTile(cellPos, _dirtTile);
                        break;

                    case '=': // 空中足場
                        _tilemap.SetTile(cellPos, _platformTile != null ? _platformTile : _grassTile);
                        break;
                }
            }
        }
        Debug.Log("地形（タイルマップ）の先行生成が完了しました。");
        MapGenerate.OnNext(Unit.Default);
    }

    private void GenerateObjectPlayerGenerate()
    {
        if (_stageLines == null) return;

        for (int y = 0; y < _mapHeight; y++)
        {
            string line = _stageLines[y];
            for (int x = 0; x < _maxMapWidth; x++)
            {
                char tileChar = (x < line.Length) ? line[x] : '.';
                int worldY = _mapHeight - 1 - y; 
                Vector3Int cellPos = new Vector3Int(x, worldY, 0);
                
                // 💡 すでに足場のタイルは敷かれているので、安全にセル座標からワールド座標に変換できる
                Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

                switch (tileChar)
                {
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
                }
            }
        }
        Debug.Log("プレイヤー、ゴールの配置が安全に完了しました！");
    }

    private void GenerateObjectsOnly()
    {
        if (_stageLines == null) return;

        for (int y = 0; y < _mapHeight; y++)
        {
            string line = _stageLines[y];
            for (int x = 0; x < _maxMapWidth; x++)
            {
                char tileChar = (x < line.Length) ? line[x] : '.';
                int worldY = _mapHeight - 1 - y; 
                Vector3Int cellPos = new Vector3Int(x, worldY, 0);
                
                // 💡 すでに足場のタイルは敷かれているので、安全にセル座標からワールド座標に変換できる
                Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

                switch (tileChar)
                {
                    case 'E': // 敵
                        GameObject _spawnEnemy = Instantiate(_enemyPrefab, worldPos, Quaternion.identity);
                        _enemyObj.OnNext(_spawnEnemy);
                        break;
                }
            }
        }
        Debug.Log("敵の配置が安全に完了しました！");
    }
}
