using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class tWordsScript : MonoBehaviour
{
		public KMAudio Audio;
		public KMBombInfo bomb;
		public KMSelectable[] words;
		public TextMesh[] wordsText;
		public GameObject disableWords;
		public KMSelectable ledButton;

		public Material[] ledOptions;
		public Renderer led;
		private int ledIndex = 0;

		public String[] column1;
		public String[] column2;
		public String[] column3;
		public String[] column4;
		public String[] column5;

		private String[] correctColumn = new String[20];
		private List <int> chosenWordsIndices = new List <int>();
		private List <int> chosenWordsIndicesOrdered = new List <int>();
		private List <string> pressedWords = new List <string>();

		private String correctAnswer = "";

		public Color[] fontColours;
		public AudioClip[] sounds;
		public Font[] fontOptions;
		public Material[] fontMaterials;
		private int fontSetting = 0;

		//Logging
    static int moduleIdCounter = 1;
    int moduleId;
		private bool colourPicked;
    private bool moduleSolved;
		private int stage = 0;
		private bool incorrect;
		private bool flashing;

		void Awake()
		{
				moduleId = moduleIdCounter++;
				foreach (KMSelectable word in words)
				{
						KMSelectable pressedWord = word;
						word.OnInteract += delegate () { WordPress(pressedWord); return false; };
				}
				ledButton.OnInteractEnded += delegate () { PressLED();};
		}

		void Start()
		{
				if(!colourPicked)
				{
						foreach(TextMesh word in wordsText)
						{
								word.font = fontOptions[fontSetting];
								word.GetComponent<Renderer>().material = fontMaterials[fontSetting];
						}
						colourPicked = true;
						PickLEDColour();
						DetermineList();
				}
				PickWords();
				DetermineOrder();
		}

		void PickLEDColour()
		{
				ledIndex = UnityEngine.Random.Range(0,5);
				led.material = ledOptions[ledIndex];
				Debug.LogFormat("[T-Words #{0}] The LED is {1}.", moduleId, ledOptions[ledIndex].name);
		}

		void DetermineList()
		{
				//Use if there are more than four batteries and the LED is not red.
				if(bomb.GetBatteryCount() > 4 && ledIndex != 3)
				{
						for(int i = 0; i <= 19; i++)
						{
								correctColumn[i] = column1[i];
						}
						Debug.LogFormat("[T-Words #{0}] The correct column is #1.", moduleId);
				}
				//Otherwise, use if there is an (unlit BOB || unlit FRK || a lit CAR || lit IND) && (the LED is not green).
				else if((bomb.IsIndicatorOff("BOB") || bomb.IsIndicatorOff("FRK") || bomb.IsIndicatorOn("CAR") || bomb.IsIndicatorOn("IND")) && (ledIndex != 1))
				{
						for(int i = 0; i <= 19; i++)
						{
								correctColumn[i] = column2[i];
						}
						Debug.LogFormat("[T-Words #{0}] The correct column is #2.", moduleId);
				}
				//Otherwise, use if there is a serial port and a DVI-D port and the LED is not blue.
				else if(bomb.GetPortCount(Port.Serial) >= 1 && bomb.GetPortCount(Port.DVI) >= 1 && ledIndex != 0)
				{
						for(int i = 0; i <= 19; i++)
						{
								correctColumn[i] = column3[i];
						}
						Debug.LogFormat("[T-Words #{0}] The correct column is #3.", moduleId);
				}
				//Otherwise, use if the serial number does not contain a vowel and the LED is not orange.
				else if(bomb.GetSerialNumberLetters().All(x => x != 'A' && x != 'E' && x != 'I' && x != 'O' && x != 'U') && (ledIndex != 2))
				{
						for(int i = 0; i <= 19; i++)
						{
								correctColumn[i] = column4[i];
						}
						Debug.LogFormat("[T-Words #{0}] The correct column is #4.", moduleId);
				}
				else
				{
						for(int i = 0; i <= 19; i++)
						{
								correctColumn[i] = column5[i];
						}
						Debug.LogFormat("[T-Words #{0}] The correct column is #5.", moduleId);
				}
		}

		void PickWords()
		{
				for(int i = 0; i <= 3; i++)
				{
						int index = UnityEngine.Random.Range(0,20);
						while(chosenWordsIndices.Contains(index))
						{
								index = UnityEngine.Random.Range(0,20);
						}
						chosenWordsIndices.Add(index);
						wordsText[i].text = correctColumn[index];
						wordsText[i].color = fontColours[0];
				}
				Debug.LogFormat("[T-Words #{0}] Your chosen words from top to bottom are: {1}, {2}, {3} & {4}.", moduleId, correctColumn[chosenWordsIndices[0]], correctColumn[chosenWordsIndices[1]], correctColumn[chosenWordsIndices[2]], correctColumn[chosenWordsIndices[3]]);;
		}

		void DetermineOrder()
		{
				chosenWordsIndicesOrdered = chosenWordsIndices.ToList();
				chosenWordsIndicesOrdered.Sort();
				Debug.LogFormat("[T-Words #{0}] The correct order is: {1}, {2}, {3} & {4}.", moduleId, correctColumn[chosenWordsIndicesOrdered[0]], correctColumn[chosenWordsIndicesOrdered[1]], correctColumn[chosenWordsIndicesOrdered[2]], correctColumn[chosenWordsIndicesOrdered[3]]);;
		}

		void WordPress(KMSelectable word)
		{
				if(moduleSolved || flashing || pressedWords.Contains(word.GetComponentInChildren<TextMesh>().text))
				{
						return;
				}
				word.AddInteractionPunch();
				Audio.PlaySoundAtTransform(sounds[stage].name, transform);
				pressedWords.Add(word.GetComponentInChildren<TextMesh>().text);
				word.GetComponentInChildren<TextMesh>().color = fontColours[1];
				correctAnswer = correctColumn[chosenWordsIndicesOrdered[stage]];
				Debug.LogFormat("[T-Words #{0}] You pressed {1}.", moduleId, word.GetComponentInChildren<TextMesh>().text);
				if(word.GetComponentInChildren<TextMesh>().text != correctAnswer)
				{
						incorrect = true;
				}
				stage++;
				if(stage == 4)
				{
						stage = 0;
						if(!incorrect)
						{
								Audio.PlaySoundAtTransform(sounds[4].name, transform);
								moduleSolved = true;
								GetComponent<KMBombModule>().HandlePass();
								Debug.LogFormat("[T-Words #{0}] Module solved.", moduleId);
								flashing = true;
								StartCoroutine(Solved());
						}
						else
						{
								Audio.PlaySoundAtTransform(sounds[5].name, transform);
								incorrect = false;
								GetComponent<KMBombModule>().HandleStrike();
								Debug.LogFormat("[T-Words #{0}] Strike! You did not enter the words in the correct order.", moduleId);
								chosenWordsIndicesOrdered.Clear();
								chosenWordsIndices.Clear();
								pressedWords.Clear();
								flashing = true;
								StartCoroutine(Strike());
						}
				}
		}

		void PressLED()
		{
				if(moduleSolved)
				{
						return;
				}
				ledButton.AddInteractionPunch(0.5f);
				Audio.PlaySoundAtTransform("beep", transform);
				if(fontSetting == 0)
				{
						foreach(TextMesh word in wordsText)
						{
								word.font = fontOptions[1];
								word.GetComponent<Renderer>().material = fontMaterials[1];
								fontSetting = 1;
						}
				}
				else if (fontSetting == 1)
				{
						foreach(TextMesh word in wordsText)
						{
								word.font = fontOptions[2];
								word.GetComponent<Renderer>().material = fontMaterials[2];
								fontSetting = 2;
						}
				}
				else
				{
						foreach(TextMesh word in wordsText)
						{
								word.font = fontOptions[0];
								word.GetComponent<Renderer>().material = fontMaterials[0];
								fontSetting = 0;
						}
				}
		}

		IEnumerator Solved()
		{
				int flash = 0;
				while(flash < 10)
				{
						foreach(TextMesh word in wordsText)
						{
								word.color = fontColours[1];
						}
						yield return new WaitForSeconds(0.05f);
						foreach(TextMesh word in wordsText)
						{
								word.color = fontColours[3];
						}
						yield return new WaitForSeconds(0.05f);
						flash++;
				}
				disableWords.SetActive(false);
				flashing = false;
		}

		IEnumerator Strike()
		{
				int flash = 0;
				while(flash < 10)
				{
						foreach(TextMesh word in wordsText)
						{
								word.color = fontColours[2];
						}
						yield return new WaitForSeconds(0.05f);
						foreach(TextMesh word in wordsText)
						{
								word.color = fontColours[3];
						}
						yield return new WaitForSeconds(0.05f);
						flash++;
				}
				flashing = false;
				Start();
		}
}
