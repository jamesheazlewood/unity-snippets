using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// The selection process will prioritise Z/distance order for picking the element over any other factor
// It's only when the Z/distance is the same is when we have to try and figure out which item to select
// based on hierarchy position and sibling order - like the renderer does to choose which UI element
// to draw in front of other UI elements
//
// Limits:
// - Doesn't work inside prefab editor
// - You need to click somewhere inside the Scene window before it will work
// - Because this prioritises distance/Z position, these elements will always be selected even if
//   they might drawn over other elements. Anything with equal distance/z will use the deepest child
public class ObjectUnderMouse : EditorWindow {
	// If useDistance is true, the window will use the distance from the camera. 
	// if useDistance is false, the window will use the Z position of the UI element in world space.
	// This isn't working 100% as the distance is from the bottom left of the UI element
	// But it might also not be ideal to use the Z position, so use whichever one works best for you.
	private readonly bool useDistance = false;

	// If useLog is true, the window will log some debug info into itself. Useful for seeing which
	// elements are active under the mouse and is useful for debugging.
	private readonly bool useLog = true;

	// If true the window will log to the console. Turn on "monospace" mode for a more readable output.
	private readonly bool logToConsole = false;

	// stores logs until they can br printed
	private List<string> logLines;

	//////////////////////////////////////////////////////////////////////////////////////////////////

	// Creates the Window
	[MenuItem("Window/UI Element Under Mouse Picker")]
	public static void ShowWindow() {
		_ = EditorWindow.GetWindow<ObjectUnderMouse>("UI Element Under Mouse Picker");
	}

	// 
	void OnEnable() {
		SceneView.duringSceneGui += OnSceneGUI;
		if(useLog) {
			logLines = new List<string>();
		}
	}

	// 
	void OnDisable() {
		SceneView.duringSceneGui -= OnSceneGUI;
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////

	// 
	private double GetRenderPriorityIncrement(Transform transform) {
		// Are we inside the canvas's direct children?
		if(transform.parent.parent == null) {
			return 1.0;
		}

		double parentScore = GetRenderPriorityIncrement(transform.parent);

		return parentScore / (transform.parent.childCount + 1);
	}

	// Gets the "order" double eg:
	// Canvas: 0.0000
	// - First canvas child: 1.0000
	// - - First child of that: 1.100
	// - - Some other thing 10 of: 1.2000
	// - - - Another 1 deep: 1.2100
	// - - etc : 1.xxxx
	// - Second canvas child 2.0000
	// The thing that has no parent, eg Canvas, counts in ints
	// Everything that does have a parent, gets its number from its parent, 
	// plus the child index divided by number of children (+1) so that it never equals the next uncle
	private double GetRenderPriority(Transform transform) {
		// Check if one of the things IS the canvas
		if(transform.parent == null) {
			return 0.0;
		}

		// Are we inside the canvas's direct children?
		if(transform.parent.parent == null) {
			return transform.GetSiblingIndex() + 1.0;
		}

		// Calculate this increment
		double parentScore = GetRenderPriority(transform.parent);
		double increment = GetRenderPriorityIncrement(transform);
		double currentAddon = increment * (transform.GetSiblingIndex() + 1.0);

		return parentScore + currentAddon;
	}

	// 
	private int GetChildDepth(Transform transform) {
		if(transform.parent == null) {
			return 0;
		}
		return GetChildDepth(transform.parent) + 1;
	}

	// 
	private void Log(string logText) {
		if(useLog) {
			logLines.Add(logText);
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////

	// 
	void OnSceneGUI(SceneView sceneView) {
		Event e = Event.current;

		Camera sceneCamera = SceneView.lastActiveSceneView.camera;
		Rect viewportRect = sceneCamera.pixelRect;
		Rect cursorRect = new Rect(0, 0, viewportRect.width, viewportRect.height);

		if(e.type == EventType.Repaint) {
			EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Arrow);
		}

		// Check for User pressing 1 on keyboard (You can hold it down - although slightly slow)
		if(e.isKey && e.type == EventType.KeyDown && e.keyCode == KeyCode.Alpha1) {
			Repaint();
			logLines.Clear();

			Vector2 mousePosition = new Vector2(
				e.mousePosition.x,
				sceneCamera.pixelHeight - e.mousePosition.y
			);

			RectTransform[] uiElements = Resources.FindObjectsOfTypeAll<RectTransform>();
			List<RectTransform> elementsUnderMouse = new List<RectTransform>();
			int longestNameLength = 0;
			float closestDistance = float.MaxValue;

			Log("Mouse position: " + mousePosition.x.ToString("0") + ", " + mousePosition.y.ToString("0"));

			foreach(RectTransform element in uiElements) {
				if(element == null) {
					continue;
				}

				if(!element.gameObject.activeInHierarchy) {
					continue;
				}

				if(!RectTransformUtility.RectangleContainsScreenPoint(element, mousePosition, sceneCamera)) {
					continue;
				}

				if(useDistance) {
					Vector3 heading = element.position - sceneCamera.transform.position;
					float distance = Vector3.Dot(heading, sceneCamera.transform.forward);
					if(distance < closestDistance) {
						closestDistance = distance;
					}
				} else {
					if(element.transform.position.z < closestDistance) {
						closestDistance = element.transform.position.z;
					}
				}

				if(element.name.Length > longestNameLength) {
					longestNameLength = element.name.Length;
				}

				elementsUnderMouse.Add(element);
			}

			// Early escape to reduce calculations
			if(elementsUnderMouse.Count == 0) {
				// Don't do anything
				Log("Nothing under mouse.");
				return;
			}

			// Print out UI Elements under mouse ponter
			if(useLog || logToConsole) {
				foreach(RectTransform rectTransform in elementsUnderMouse) {
					Vector3 heading = rectTransform.position - sceneCamera.transform.position;
					float distance = Vector3.Dot(heading, sceneCamera.transform.forward);

					// This is only used here for now
					int childDepth = GetChildDepth(rectTransform.transform);

					// Depth score is onle here for debugging
					double depthScore = GetRenderPriority(rectTransform.transform);

					// This is only used to pad left the element name
					string nameString = "'" + rectTransform.name + "'";
					Log(
						nameString.PadLeft(longestNameLength + 2) +
						" Pos Z: " + rectTransform.transform.position.z.ToString("0.0000") +
						" Sibling index: " + rectTransform.transform.GetSiblingIndex().ToString("000") +
						" Dist: " + distance.ToString("0.0000") +
						" Children: " + rectTransform.transform.childCount.ToString("000") +
						" Child depth: " + childDepth.ToString("000") +
						" Depth score: " + depthScore.ToString("00.00000000")
					);
				}
			}

			// Quick exit if there's only 1 element under mouse
			if(elementsUnderMouse.Count == 1) {
				Log("Only 1 under mouse, picking... " + elementsUnderMouse[0].gameObject);
				Selection.activeObject = elementsUnderMouse[0].gameObject;
				return;
			}

			// Make list of only the closest
			List<RectTransform> closestElementsUnderMouse = new List<RectTransform>();
			foreach(RectTransform rectTransform in elementsUnderMouse) {
				// Add all elements to a list that are the same distance,
				// that distance being the closest one to the camera from the previous loop
				if(useDistance) {
					Vector3 heading = rectTransform.position - sceneCamera.transform.position;
					float distance = Vector3.Dot(heading, sceneCamera.transform.forward);
					if(distance == closestDistance) {
						closestElementsUnderMouse.Add(rectTransform);
					}
				} else {
					if(rectTransform.position.z == closestDistance) {
						closestElementsUnderMouse.Add(rectTransform);
					}
				}
			}

			// Quick exit if there's only 1 closest
			if(closestElementsUnderMouse.Count == 1) {
				Log("Only 1 closest, picking... " + closestElementsUnderMouse[0].gameObject);
				Selection.activeObject = closestElementsUnderMouse[0].gameObject;
				return;
			}

			Log("Getting the object with the highest draw score...");

			// If we still don't have just 1 possibility, 
			// get the object with the highest depth in the hierarchy
			double highestScore = 0;
			RectTransform highestScoreRectTransform = null;

			// Loop through elements and calculate the highest hierarchy score
			foreach(RectTransform rectTransform in closestElementsUnderMouse) {
				double score = GetRenderPriority(rectTransform.transform);

				if(score > highestScore) {
					highestScore = score;
					highestScoreRectTransform = rectTransform;
				}
			}

			Log("Highest Score: " + highestScore + ", picking... " + highestScoreRectTransform.gameObject);

			Selection.activeObject = highestScoreRectTransform.gameObject;
		}
	}

	// 
	void OnGUI() {
		if(useLog) {
			if(logToConsole && logLines.Count > 0) {
				Debug.Log("---------------------------------------");
			}

			foreach(string line in logLines) {
				if(logToConsole) {
					Debug.Log(line);
				}
				GUILayout.Label(line.TrimStart());
			}
		}
	}
}
