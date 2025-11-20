using Framework.HUD.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour, IHudSystemCallback
{
    enum EPlanType
    {
        NewHud,
        UGUIHud,
    }
    public HudObject hudObject;

    public Transform uguiRoot;
    public GameObject uguiHud;

    public int spawnCnt=1000;
    private HudSystem m_pHudSystem;

    public List<Sprite> headIcons;
    public List<string> nameTests;

    private List<GameObject> m_UguiHuds = new List<GameObject>();
    EPlanType m_eType = EPlanType.NewHud;

    private GUIStyle m_style = new GUIStyle();

    private void Awake()
    {
        m_pHudSystem = new HudSystem();
        m_pHudSystem.SetRenderCamera(Camera.main);
        m_pHudSystem.RegisterCallback(this);

        Application.targetFrameRate = 120;

        m_style.fontSize = 40;
        m_style.normal.textColor = Color.cyan;
    }
    // Start is called before the first frame update
    void Start()
    {
        HudNewPlan();
    }

    // Update is called once per frame
    void Update()
    {
        uguiRoot.rotation = Quaternion.Euler(0, Time.time * 5, 0);
        transform.rotation = Quaternion.Euler(0, Time.time * 5, 0);
        if(m_eType == EPlanType.NewHud)
        {
            m_pHudSystem.Update();
            m_pHudSystem.Render();
        }
    }

    private void LateUpdate()
    {
        if (m_eType == EPlanType.NewHud)
            m_pHudSystem.LateUpdate();
    }

    private void OnDestroy()
    {
        m_pHudSystem.Destroy();
    }

    public bool OnSpawnInstance(AWidget pWidget, string strParticle, System.Action<GameObject> onCallback)
    {
#if UNITY_EDITOR
        onCallback(GameObject.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(strParticle)));
#endif
        return true;
    }

    public bool OnDestroyInstance(AWidget pWidget, GameObject pGameObject)
    {
        if (Application.isPlaying) UnityEngine.GameObject.Destroy(pGameObject);
        else UnityEngine.GameObject.DestroyImmediate(pGameObject);
        return true;
    }

    void ClearHuds()
    {
        m_pHudSystem.ClearHuds();
        foreach (var db in m_UguiHuds)
        {
            if (Application.isPlaying) UnityEngine.GameObject.Destroy(db);
            else UnityEngine.GameObject.DestroyImmediate(db);
        }
    }

    public void HudNewPlan()
    {
        ClearHuds();
        m_eType = EPlanType.NewHud;
        for (int i = 0; i < spawnCnt; i++)
        {
            GameObject go = new GameObject();
            go.hideFlags |= HideFlags.HideAndDontSave;
            go.transform.SetParent(transform, false);

            // 随机本地坐标
            Vector3 dir = Random.onUnitSphere;
            float len = Random.Range(0f, 50f);
            Vector3 localPos = dir * len;

            go.transform.localPosition = localPos;

            var hud = m_pHudSystem.CreateHud(hudObject);
            hud.OffsetPosition = Vector3.zero; // 让HUD跟随目标Transform
            hud.SetFollowTarget(go.transform);

            HudImage icon = hud.GetWidgetById<HudImage>(2);
            if (icon != null) icon.SetSprite(headIcons[UnityEngine.Random.Range(0, headIcons.Count)]);
            HudText name = hud.GetWidgetById<HudText>(5);
            if (name != null)
            {
                name.SetText(nameTests[UnityEngine.Random.Range(0, nameTests.Count)] + "_" + i);
                name.SetColor(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f));
            }
        }
    }

    public void UGUIPlane()
    {
        ClearHuds();
        m_eType = EPlanType.UGUIHud;
        for (int i = 0; i < spawnCnt; i++)
        {
            GameObject go = GameObject.Instantiate(uguiHud);

            UGUIHud hud = go.GetComponent<UGUIHud>();
            go.hideFlags |= HideFlags.HideAndDontSave;
            go.transform.SetParent(uguiRoot, false);

            // 随机本地坐标
            Vector3 dir = Random.onUnitSphere;
            float len = Random.Range(0f, 250f);
            Vector3 localPos = dir * len;

            go.transform.localPosition = localPos;

            if (hud.icon != null) hud.icon.sprite =headIcons[UnityEngine.Random.Range(0, headIcons.Count)];
            if (hud.Name != null)
            {
                hud.Name.text =nameTests[UnityEngine.Random.Range(0, nameTests.Count)] + "_" + i;
                hud.Name.color =new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            }
            m_UguiHuds.Add(go);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(0, 60, 200, 100), "Count: " + spawnCnt + "  方案:" + m_eType.ToString(), m_style);
    }
}
