using Framework.HUD.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour, IHudSystemCallback
{
    public HudObject hudObject;
    public int spawnCnt=1000;
    private HudSystem m_pHudSystem;

    public List<Sprite> headIcons;
    public List<string> nameTests;
    private void Awake()
    {
        m_pHudSystem = new HudSystem();
        m_pHudSystem.SetRenderCamera(Camera.main);
        m_pHudSystem.RegisterCallback(this);
    }
    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Time.time * 5, 0);
        m_pHudSystem.Update();
        m_pHudSystem.Render();
    }

    private void LateUpdate()
    {
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
}
