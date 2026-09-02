using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TextMapLoader : MonoBehaviour
{
    private static readonly Subject<Unit> _mapGenerate = new();
    public static Subject<Unit> MapGenerate => _mapGenerate;

    // GameManagerがそのまま購読できるように static のまま維持
    private static readonly Subject<GameObject> _enemyObj = new Subject<GameObject>();
    public static Subject<GameObject> EnemyObj => _enemyObj;

    [Header("ーー 使用するアセット（Tile） ーー")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileBase _grassTile;     
    [SerializeField] private TileBase _dirtTile;      
    [SerializeField] private TileBase _platformTile;  

    [Header("ーー 使用するアセット（Prefab） ーー")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private GameObject _goalPrefab;
    [SerializeField] private GameObject _deathPrefab;
    [SerializeField] private GameObject _fieldPrefab;

    [Header("ーー 読み込むステージのテキストファイル名 ーー")]
    [SerializeField] private string _stageFileName = "Stage1"; 

    private string[] _stageLines;
    private int _mapHeight;
    private int _maxMapWidth;

    void Start()
    {
        _tilemap.ClearAllTiles();
        ParseTextData();

        // 💡 プレイヤーの生成・配置
        GameManager.PlayerGenerate
            .Take(1) // リトライ時の多重実行を防ぐ安全策
            .Subscribe(_ =>
            {
                Debug.Log("プレイヤースポーンを生成");
                GeneratePlayerOnly();
            }).AddTo(this);

        // 💡 敵の生成
        GameManager.EnemyGenerate
            .Take(1) // 多重生成を防ぐ
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
        
        // 💡 タイルを敷くタイミングで、動かない固定オブジェクト（ゴール等）も一緒に生成してしまう
        GenerateTilemapAndStaticObjects();
    }

    // 💡 タイルと固定オブジェクト（ゴール・死亡判定・フィールド）を1回だけ生成する
    private void GenerateTilemapAndStaticObjects()
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
                Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

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
                                if (upperTileChar == '.' || upperTileChar == 'E' || upperTileChar == 'P' || upperTileChar == '='|| upperTileChar == 'G' || upperTileChar == 'D'|| upperTileChar == 'F')
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

                    // 💡 ゴール・死亡判定などは、プレイヤーの通知とは切り離してここで1回だけ生成
                    case 'G': 
                        Instantiate(_goalPrefab, worldPos, Quaternion.identity);
                        break;
                    case 'D': 
                        Instantiate(_deathPrefab, worldPos, Quaternion.identity);
                        break;
                    case 'F': 
                        Instantiate(_fieldPrefab, worldPos, Quaternion.identity);
                        break;
                }
            }
        }
        MapGenerate.OnNext(Unit.Default);
    }

    // 💡 プレイヤーの生成・位置変更だけを純粋に行う
    private void GeneratePlayerOnly()
    {
        if (_stageLines == null) return;

        for (int y = 0; y < _mapHeight; y++)
        {
            string line = _stageLines[y];
            for (int x = 0; x < _maxMapWidth; x++)
            {
                char tileChar = (x < line.Length) ? line[x] : '.';
                if (tileChar == 'P')
                {
                    int worldY = _mapHeight - 1 - y; 
                    Vector3Int cellPos = new Vector3Int(x, worldY, 0);
                    Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

                    if (Player.Instance == null)
                    {
                        Instantiate(_playerPrefab, worldPos, Quaternion.identity);
                    }
                    else
                    {
                        Player.Instance.transform.position = worldPos;
                    }
                    return; // プレイヤーが見つかったら終了
                }
            }
        }
    }

    // 💡 敵の生成（元のまま）
    private void GenerateObjectsOnly()
    {
        if (_stageLines == null) return;

        for (int y = 0; y < _mapHeight; y++)
        {
            string line = _stageLines[y];
            for (int x = 0; x < _maxMapWidth; x++)
            {
                char tileChar = (x < line.Length) ? line[x] : '.';
                if (tileChar == 'E')
                {
                    int worldY = _mapHeight - 1 - y; 
                    Vector3Int cellPos = new Vector3Int(x, worldY, 0);
                    Vector3 worldPos = _tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);

                    GameObject _spawnEnemy = Instantiate(_enemyPrefab, worldPos, Quaternion.identity);
                    _enemyObj.OnNext(_spawnEnemy);
                }
            }
        }
    }
}
