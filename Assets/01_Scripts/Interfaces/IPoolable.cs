/// <summary>
/// 오브젝트 풀에서 꺼내거나 반환할 때 필요한 생명주기를 정의합니다.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 오브젝트 풀에서 꺼내 사용할 때 호출합니다.
    /// </summary>
    void OnSpawned();

    /// <summary>
    /// 사용을 마치고 오브젝트 풀로 반환할 때 호출합니다.
    /// </summary>
    void OnDespawned();
}
