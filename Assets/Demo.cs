using Framework.HUD.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
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
    }
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < spawnCnt; i++)
        {
            GameObject go = new GameObject();
            go.hideFlags |= HideFlags.DontSave;
            go.transform.SetParent(transform, false);

            var hud = m_pHudSystem.CreateHud(hudObject);
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            dir = dir.normalized;
            float len = Random.Range(12, 50);
            hud.OffsetPosition = dir * len;
            go.transform.position = dir * len;
            hud.SetFollowTarget(go.transform);

            HudImage icon = hud.GetWidgetById<HudImage>(2);
            if (icon != null) icon.SetSprite(headIcons[UnityEngine.Random.Range(0, headIcons.Count)]);
            HudText name = hud.GetWidgetById<HudText>(5);
            if (name != null) name.SetText(nameTests[UnityEngine.Random.Range(0, nameTests.Count)] + "_" + i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_pHudSystem.Update();
        m_pHudSystem.Render();
    }

    private void LateUpdate()
    {
        m_pHudSystem.LateUpdate();
        transform.rotation = Quaternion.Euler(0, Time.time * 10, 0);
    }

    private void OnDestroy()
    {
        m_pHudSystem.Destroy();
    }
}
