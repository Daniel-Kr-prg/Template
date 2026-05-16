using UnityEditor;
using UnityEngine;

namespace DanieloZ.WorldInteraction.Editor
{
    [CustomEditor(typeof(WorldCameraZoomRig))]
    public sealed class WorldCameraZoomRigEditor : UnityEditor.Editor
    {
        private SerializedProperty groundPivot;
        private SerializedProperty movableRoot;
        private SerializedProperty pivotIndicator;
        private SerializedProperty zoom;
        private SerializedProperty useStartZoom;
        private SerializedProperty startZoom;
        private SerializedProperty zoomStep;
        private SerializedProperty zoomLerpSpeed;
        private SerializedProperty cameras;
        private SerializedProperty curvePositionRelativeToPivot;
        private SerializedProperty cameraPositionLerpSpeed;
        private SerializedProperty cameraRotationLerpSpeed;
        private SerializedProperty cameraFovLerpSpeed;
        private SerializedProperty avoidCameraCollision;
        private SerializedProperty cameraCollisionMask;
        private SerializedProperty cameraCollisionRadius;
        private SerializedProperty cameraCollisionPadding;
        private SerializedProperty orbitMode;
        private SerializedProperty enableMiddleMouseOrbit;
        private SerializedProperty orbitDegreesPerPixel;
        private SerializedProperty orbitLerpSpeed;
        private SerializedProperty enableKeyboardOrbit;
        private SerializedProperty keyboardOrbitDegreesPerSecond;
        private SerializedProperty keyboardOrbitAcceleration;
        private SerializedProperty keyboardOrbitDeceleration;
        private SerializedProperty scaleCameraInputSpeedByZoom;
        private SerializedProperty zoomSpeedScaleCurve;
        private SerializedProperty enableKeyboardMove;
        private SerializedProperty keyboardMoveSpeed;
        private SerializedProperty keyboardMoveAcceleration;
        private SerializedProperty keyboardMoveDeceleration;
        private SerializedProperty enableScreenEdgePan;
        private SerializedProperty edgePanDistance;
        private SerializedProperty edgePanMaxSpeed;
        private SerializedProperty edgePanSpeedCurve;
        private SerializedProperty keepPivotOnGroundSurface;
        private SerializedProperty groundMask;
        private SerializedProperty groundRaycastHeight;
        private SerializedProperty groundRaycastDistance;
        private SerializedProperty groundHeightOffset;
        private SerializedProperty groundFollowSpeed;
        private SerializedProperty groundTriggerInteraction;
        private SerializedProperty pivotConstraints;
        private SerializedProperty drawGizmos;
        private SerializedProperty drawFocusLines;
        private SerializedProperty zoomPathPointRadius;
        private SerializedProperty currentCameraCurvePointColor;
        private SerializedProperty currentLookAtCurvePointColor;
        private SerializedProperty currentLookAtTargetColor;
        private SerializedProperty debugInput;
        private SerializedProperty debugStatusInterval;

        private void OnEnable()
        {
            groundPivot = serializedObject.FindProperty("groundPivot");
            movableRoot = serializedObject.FindProperty("movableRoot");
            pivotIndicator = serializedObject.FindProperty("pivotIndicator");
            zoom = serializedObject.FindProperty("zoom");
            useStartZoom = serializedObject.FindProperty("useStartZoom");
            startZoom = serializedObject.FindProperty("startZoom");
            zoomStep = serializedObject.FindProperty("zoomStep");
            zoomLerpSpeed = serializedObject.FindProperty("zoomLerpSpeed");
            cameras = serializedObject.FindProperty("cameras");
            curvePositionRelativeToPivot = serializedObject.FindProperty("curvePositionRelativeToPivot");
            cameraPositionLerpSpeed = serializedObject.FindProperty("cameraPositionLerpSpeed");
            cameraRotationLerpSpeed = serializedObject.FindProperty("cameraRotationLerpSpeed");
            cameraFovLerpSpeed = serializedObject.FindProperty("cameraFovLerpSpeed");
            avoidCameraCollision = serializedObject.FindProperty("avoidCameraCollision");
            cameraCollisionMask = serializedObject.FindProperty("cameraCollisionMask");
            cameraCollisionRadius = serializedObject.FindProperty("cameraCollisionRadius");
            cameraCollisionPadding = serializedObject.FindProperty("cameraCollisionPadding");
            orbitMode = serializedObject.FindProperty("orbitMode");
            enableMiddleMouseOrbit = serializedObject.FindProperty("enableMiddleMouseOrbit");
            orbitDegreesPerPixel = serializedObject.FindProperty("orbitDegreesPerPixel");
            orbitLerpSpeed = serializedObject.FindProperty("orbitLerpSpeed");
            enableKeyboardOrbit = serializedObject.FindProperty("enableKeyboardOrbit");
            keyboardOrbitDegreesPerSecond = serializedObject.FindProperty("keyboardOrbitDegreesPerSecond");
            keyboardOrbitAcceleration = serializedObject.FindProperty("keyboardOrbitAcceleration");
            keyboardOrbitDeceleration = serializedObject.FindProperty("keyboardOrbitDeceleration");
            scaleCameraInputSpeedByZoom = serializedObject.FindProperty("scaleCameraInputSpeedByZoom");
            zoomSpeedScaleCurve = serializedObject.FindProperty("zoomSpeedScaleCurve");
            enableKeyboardMove = serializedObject.FindProperty("enableKeyboardMove");
            keyboardMoveSpeed = serializedObject.FindProperty("keyboardMoveSpeed");
            keyboardMoveAcceleration = serializedObject.FindProperty("keyboardMoveAcceleration");
            keyboardMoveDeceleration = serializedObject.FindProperty("keyboardMoveDeceleration");
            enableScreenEdgePan = serializedObject.FindProperty("enableScreenEdgePan");
            edgePanDistance = serializedObject.FindProperty("edgePanDistance");
            edgePanMaxSpeed = serializedObject.FindProperty("edgePanMaxSpeed");
            edgePanSpeedCurve = serializedObject.FindProperty("edgePanSpeedCurve");
            keepPivotOnGroundSurface = serializedObject.FindProperty("keepPivotOnGroundSurface");
            groundMask = serializedObject.FindProperty("groundMask");
            groundRaycastHeight = serializedObject.FindProperty("groundRaycastHeight");
            groundRaycastDistance = serializedObject.FindProperty("groundRaycastDistance");
            groundHeightOffset = serializedObject.FindProperty("groundHeightOffset");
            groundFollowSpeed = serializedObject.FindProperty("groundFollowSpeed");
            groundTriggerInteraction = serializedObject.FindProperty("groundTriggerInteraction");
            pivotConstraints = serializedObject.FindProperty("pivotConstraints");
            drawGizmos = serializedObject.FindProperty("drawGizmos");
            drawFocusLines = serializedObject.FindProperty("drawFocusLines");
            zoomPathPointRadius = serializedObject.FindProperty("zoomPathPointRadius");
            currentCameraCurvePointColor = serializedObject.FindProperty("currentCameraCurvePointColor");
            currentLookAtCurvePointColor = serializedObject.FindProperty("currentLookAtCurvePointColor");
            currentLookAtTargetColor = serializedObject.FindProperty("currentLookAtTargetColor");
            debugInput = serializedObject.FindProperty("debugInput");
            debugStatusInterval = serializedObject.FindProperty("debugStatusInterval");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Section("Scene", () =>
            {
                Field(groundPivot);
                Field(movableRoot);
                Field(pivotIndicator);
            });

            Section("Zoom", () =>
            {
                Field(zoom);
                Field(useStartZoom);
                if (useStartZoom.boolValue)
                {
                    Field(startZoom);
                }

                Field(zoomStep);
                Field(zoomLerpSpeed);
            });

            DrawCameraEntries();

            Section("Motion", () =>
            {
                Field(curvePositionRelativeToPivot);
                Field(cameraPositionLerpSpeed);
                Field(cameraRotationLerpSpeed);
                Field(cameraFovLerpSpeed);
            });

            Section("Collision", () =>
            {
                Field(avoidCameraCollision);
                if (avoidCameraCollision.boolValue)
                {
                    Field(cameraCollisionMask);
                    Field(cameraCollisionRadius);
                    Field(cameraCollisionPadding);
                }
            });

            Section("Orbit", () =>
            {
                Field(orbitMode);
                Field(orbitLerpSpeed);
                Field(enableMiddleMouseOrbit);
                if (enableMiddleMouseOrbit.boolValue)
                {
                    Field(orbitDegreesPerPixel);
                }

                Field(enableKeyboardOrbit);
                if (enableKeyboardOrbit.boolValue)
                {
                    Field(keyboardOrbitDegreesPerSecond);
                    Field(keyboardOrbitAcceleration);
                    Field(keyboardOrbitDeceleration);
                }
            });

            Section("Input Speed", () =>
            {
                Field(scaleCameraInputSpeedByZoom);
                if (scaleCameraInputSpeedByZoom.boolValue)
                {
                    Field(zoomSpeedScaleCurve);
                }
            });

            Section("Map Movement", () =>
            {
                Field(enableKeyboardMove);
                if (enableKeyboardMove.boolValue)
                {
                    Field(keyboardMoveSpeed);
                    Field(keyboardMoveAcceleration);
                    Field(keyboardMoveDeceleration);
                }

                Field(enableScreenEdgePan);
                if (enableScreenEdgePan.boolValue)
                {
                    Field(edgePanDistance);
                    Field(edgePanMaxSpeed);
                    Field(edgePanSpeedCurve);
                }
            });

            Section("Ground And Bounds", () =>
            {
                Field(keepPivotOnGroundSurface);
                if (keepPivotOnGroundSurface.boolValue)
                {
                    Field(groundMask);
                    Field(groundRaycastHeight);
                    Field(groundRaycastDistance);
                    Field(groundHeightOffset);
                    Field(groundFollowSpeed);
                    Field(groundTriggerInteraction);
                }

                Field(pivotConstraints, true);
            });

            Section("Debug", () =>
            {
                Field(drawGizmos);
                if (drawGizmos.boolValue)
                {
                    Field(drawFocusLines);
                    Field(zoomPathPointRadius);
                    Field(currentCameraCurvePointColor);
                    Field(currentLookAtCurvePointColor);
                    Field(currentLookAtTargetColor);
                }

                Field(debugInput);
                if (debugInput.boolValue)
                {
                    Field(debugStatusInterval);
                }
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCameraEntries()
        {
            Section("Camera Entries", () =>
            {
                var size = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Size", "Number of zoom camera entries."), cameras.arraySize));
                if (size != cameras.arraySize)
                {
                    cameras.arraySize = size;
                }

                for (var i = 0; i < cameras.arraySize; i++)
                {
                    var entry = cameras.GetArrayElementAtIndex(i);
                    DrawCameraEntry(entry, i);
                }
            });
        }

        private void DrawCameraEntry(SerializedProperty entry, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, $"Entry {index}", true);
            if (GUILayout.Button("Remove", GUILayout.Width(72f)))
            {
                cameras.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            if (!entry.isExpanded)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.indentLevel++;

            var cameraReferenceMode = entry.FindPropertyRelative("cameraReferenceMode");
            var cameraId = entry.FindPropertyRelative("cameraId");
            var virtualCamera = entry.FindPropertyRelative("virtualCamera");
            var rangeStart = entry.FindPropertyRelative("rangeStart");
            var rangeEnd = entry.FindPropertyRelative("rangeEnd");
            var movingTargetMode = entry.FindPropertyRelative("movingTargetMode");
            var movingTransform = entry.FindPropertyRelative("movingTransform");
            var zoomCurve = entry.FindPropertyRelative("zoomCurve");
            var normalizeZoomForCurve = entry.FindPropertyRelative("normalizeZoomForCurve");
            var lookAtCurve = entry.FindPropertyRelative("lookAtCurve");
            var lookAtCurveRelativeToPivot = entry.FindPropertyRelative("lookAtCurveRelativeToPivot");
            var normalizeLookAtZoomForCurve = entry.FindPropertyRelative("normalizeLookAtZoomForCurve");
            var driveFov = entry.FindPropertyRelative("driveFov");
            var fovCurve = entry.FindPropertyRelative("fovCurve");

            Field(cameraReferenceMode);
            if (cameraReferenceMode.enumValueIndex == 0)
            {
                Field(cameraId);
            }
            else
            {
                Field(virtualCamera);
            }

            Field(rangeStart);
            Field(rangeEnd);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            Field(movingTargetMode);
            if (movingTargetMode.enumValueIndex == 1)
            {
                Field(movingTransform);
            }

            Field(zoomCurve);
            Field(normalizeZoomForCurve);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Look At", EditorStyles.boldLabel);
            Field(lookAtCurve);
            if (lookAtCurve.objectReferenceValue != null)
            {
                Field(lookAtCurveRelativeToPivot);
                Field(normalizeLookAtZoomForCurve);
            }

            EditorGUILayout.Space(3f);
            Field(driveFov);
            if (driveFov.boolValue)
            {
                Field(fovCurve);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void Section(string title, System.Action draw)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            draw.Invoke();
            EditorGUILayout.EndVertical();
        }

        private static void Field(SerializedProperty property, bool includeChildren = false)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, property.tooltip), includeChildren);
        }
    }
}
