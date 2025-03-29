#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-100)]
public class UI_Management_Positioning : MonoBehaviour
{
    RectTransform rt;

    public RectTransform Target;
    public string DisplayName;

    public bool autoSet;

    bool initialized = false;

    RectTransform oldParent;

    Vector2 oldPivot;

    Vector3 oldPosition;
    Quaternion oldRotation;
    Vector3 oldScale;

    Vector2 oldOffsetMin;
    Vector2 oldOffsetMax;

    void Awake()
    {
        if (Target == null)
        {
            Debug.LogWarning("NO ORIGINAL RECT SET FOR RectTransformCopier!", gameObject);
            return;
        }

        Init();

        if (autoSet)
        {
            SetPositionToTarget();
        }
        //else
        //{
        //    UI_Elements_Page page = GetComponent<UI_Elements_Page>();
        //    if (page != null)
        //    {
        //        page.RegisterOnPageShow("positioningOnShow", (x) => SetPositionToTarget());
        //        page.RegisterOnPageHide("positioningOnHide", (x) => SetPositionToOrigin());
        //    }
        //}
    }

    void Init()
    {
        initialized = true;
        rt = GetComponent<RectTransform>();
        CopyToOld();
    }

    [ContextMenu("Copy To Target")]
    public void SetPositionToTarget()
    {
        CopyFromTarget();
    }

    [ContextMenu("Copy to Origin")]
    public void SetPositionToOrigin()
    {
        CopyFromOld();
    }

    void CopyToOld()
    {
        if (!initialized)
            Init();

        oldParent = (RectTransform)rt.parent;
        oldPivot = rt.pivot;
        oldPosition = rt.position;
        oldRotation = rt.rotation;
        oldScale = rt.localScale;
        oldOffsetMax = rt.offsetMax;
        oldOffsetMin = rt.offsetMin;
    }

    void CopyFromOld()
    {
        if (!initialized)
            Init();

        rt.SetParent(oldParent);
        rt.pivot = oldPivot;
        rt.position = oldPosition;
        rt.localRotation = oldRotation;
        rt.localScale = oldScale;
        rt.offsetMin = oldOffsetMin;
        rt.offsetMax = oldOffsetMax;
    }

    void CopyFromTarget()
    {
        if (!initialized)
            Init();

        rt.SetParent(Target);
        rt.pivot = Target.pivot;
        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Target == null || Application.isPlaying)
            return;

        RectTransform me = (RectTransform)transform;

        DrawRectAroundParent();
        DrawRectAroundMe(me);

        DrawHandles(me);
    }



    void DrawRectAroundParent()
    {
        Gizmos.color = new Color(0.3f, 0.3f, 1, 0.5f);

        Vector3 pos = new Vector3(Target.position.x - Target.sizeDelta.x / 2f,
                                  Target.position.y - Target.sizeDelta.y / 2f,
                                  Target.position.z) +
                      new Vector3(Target.sizeDelta.x * Target.pivot.x,
                                  Target.sizeDelta.y * Target.pivot.y,
                                  0);

        Gizmos.DrawWireCube(pos, Target.rect.size * 1.005f * Target.lossyScale);
    }

    void DrawRectAroundMe(RectTransform me)
    {
        if (Target.rect.size == me.rect.size)
            Gizmos.color = new Color(0, 0, 1, 0.5f);
        else
            Gizmos.color = new Color(1, 0, 0);

        Gizmos.DrawWireCube(me.position, me.rect.size * Target.lossyScale);
    }

    void DrawHandles(RectTransform me)
    {
        if (SceneView.currentDrawingSceneView == null)
            return;

        Handles.BeginGUI();

        DrawLabel(me);

        Handles.EndGUI();
    }

    void DrawLabel(RectTransform me)
    {
        Vector3 labelPosition = SceneViewWorldToScreenPoint(SceneView.currentDrawingSceneView, me.position + new Vector3(me.rect.x, -me.rect.y, 0));

        //Cull texts
        if (labelPosition.x < 0 || labelPosition.x > SceneView.currentDrawingSceneView.size * 2 ||
            labelPosition.y < 0 || labelPosition.y > SceneView.currentDrawingSceneView.size * 2)
            return;

        float textScale = EditorPrefs.GetFloat("UIKIT_textScale", 9f);
        int scale = (int)(textScale * 10_000 / SceneView.currentDrawingSceneView.size);

        if (scale > 5)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(1, 1, 1, 0.3f);
            style.fontSize = scale;
            GUI.Label(new Rect(labelPosition.x, labelPosition.y - scale * 1.75f, 200, 20), string.IsNullOrEmpty(DisplayName) ? gameObject.name : DisplayName, style);
        }
    }

    //Draw a line between me and the original
    void OnDrawGizmosSelected()
    {
        if (Target == null || Application.isPlaying)
            return;

        RectTransform me = (RectTransform)transform;
        Gizmos.color = new Color(0.3f, 0.3f, 1, 0.1f);
        Gizmos.DrawLine(me.position, Target.position);
    }

    public static Vector3 SceneViewWorldToScreenPoint(SceneView sv, Vector2 worldPos)
    {
        var style = (GUIStyle)"GV Gizmo DropDown";
        Vector2 ribbon = style.CalcSize(sv.titleContent);

        Vector2 sv_correctSize = sv.position.size;
        sv_correctSize.y -= ribbon.y; //exclude this nasty ribbon

        //gives coordinate inside SceneView context.
        // WorldToViewportPoint() returns 0-to-1 value, where 0 means 0% and 1.0 means 100% of the dimension
        Vector3 pointInView = sv.camera.WorldToViewportPoint(worldPos);
        Vector3 pointInSceneView = pointInView * sv_correctSize;
        var p1 = pointInSceneView;
        p1.y = sv.position.height - p1.y;

        return p1;
    }
#endif
}