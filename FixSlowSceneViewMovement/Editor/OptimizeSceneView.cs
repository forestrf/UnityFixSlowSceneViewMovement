using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

/*
When pressing a keyboard key the operative system repeats the key press many times.
Unity decides to queue all of those repeated key presses when moving in the scene view and generates several "slow" events
that trigger OnGUI Repaints and more. So many events that the queue keeps growing if the computer can't process all of them
fast enough, to the point of keep moving after long after the keys have been released.

Events that get queued until all of them are performed. If your computer is not fast enough, it means that the editor will keep
on executing those key presses long after you stopped touching the keyboard.

This scripts changes the Windows settings of the keyboard, slowing down as much as it can the settings of key press repeat speed,
while moving on the scene. Then restores the previous settings.
*/

namespace Ashkatchap {
	public class OptimizeSceneView {
		[InitializeOnLoadMethod]
		static void Init() {
			var fixer = new OptimizeSceneView();
#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui += fixer.DuringSceneGui;
#else
			SceneView.onSceneGUIDelegate += fixer.DuringSceneGui;
#endif
			EditorApplication.playModeStateChanged += fixer.OnPlayModeState;
		}

		private bool forceFixOn;
		private void OnPlayModeState(PlayModeStateChange state) {
			switch (state) {
				case PlayModeStateChange.EnteredPlayMode:
					forceFixOn = true;
					keyboardRepetition.Modify();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					forceFixOn = false;
					keyboardRepetition.Restore();
					break;
			}
		}

		KeyboardRepetition keyboardRepetition = new KeyboardRepetition();
		void DuringSceneGui(SceneView sceneView) {
			Event current = Event.current;

			switch (current.type) {
				case EventType.MouseDown:
					keyboardRepetition.Modify();
					break;
				case EventType.MouseUp:
				case EventType.DragExited:
				case EventType.ContextClick:
				case EventType.MouseLeaveWindow:
					if (!forceFixOn) {
						keyboardRepetition.Restore();
					}
					break;
			}
		}

		~OptimizeSceneView() {
			keyboardRepetition.Restore();
		}

		unsafe class KeyboardRepetition {
			const int SPI_GETKEYBOARDDELAY = 0x0016;
			const int SPI_SETKEYBOARDDELAY = 0x0017;
			const int SPI_GETKEYBOARDSPEED = 0x000A;
			const int SPI_SETKEYBOARDSPEED = 0x000B;

			int originalKeyboardDelay = 0;
			int originalKeyboardSpeed = 31;
			int fixKeyboardDelay = 3;
			int fixKeyboardSpeed = 0;
			bool modified = false;

			public KeyboardRepetition() {
				modified = Read(SPI_GETKEYBOARDDELAY) == fixKeyboardDelay;
			}

			public void Modify() {
				if (modified) return;
				modified = true;

				originalKeyboardDelay = Read(SPI_GETKEYBOARDDELAY);
				originalKeyboardSpeed = Read(SPI_GETKEYBOARDSPEED);

				Write(SPI_SETKEYBOARDDELAY, fixKeyboardDelay);
				Write(SPI_SETKEYBOARDSPEED, fixKeyboardSpeed);
			}
			public unsafe void Restore() {
				if (!modified) return;
				modified = false;

				Write(SPI_SETKEYBOARDDELAY, originalKeyboardDelay);
				Write(SPI_SETKEYBOARDSPEED, originalKeyboardSpeed);
			}

			private int Read(int KEY) {
				int pvParam;
				int* ptr = &pvParam;
				bool success = SystemParametersInfo(KEY, 0, new IntPtr(ptr), 0);
				if (!success) Debug.LogError("There was a problem");
				//Debug.Log("Read: " + pvParam);
				return pvParam;
			}
			private void Write(int KEY, int uiParam) {
				bool success = SystemParametersInfo(KEY, uiParam, IntPtr.Zero, 0);
				if (!success) Debug.LogError("There was a problem");
				//Debug.Log("Written: " + uiParam);
			}


			[DllImport("user32.dll", SetLastError = true)]
			static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);
		}
	}
}
