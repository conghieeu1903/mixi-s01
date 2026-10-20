using UnityEngine;

// Router đặt trong nhà, cấp mạng cho máy tính và rig đào coin
public class Router : MonoBehaviour
{
    [SerializeField] RouterData data;

    public RouterData Data => data;

    public void SetData(RouterData routerData)
    {
        data = routerData;
    }
}
