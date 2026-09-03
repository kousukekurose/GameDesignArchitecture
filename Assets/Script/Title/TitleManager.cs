using UnityEngine;
using R3;

public class TitleManager : MonoBehaviour
{
    private static readonly Subject<Unit> _onTitleStartSubject = new();
    public static Observable<Unit> OnTitleStart => _onTitleStartSubject;
    
    private void Start()
    {
        _onTitleStartSubject.OnNext(Unit.Default);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
