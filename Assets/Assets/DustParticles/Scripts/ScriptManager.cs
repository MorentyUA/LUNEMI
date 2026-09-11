using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Handler
{ 
public class ScriptManager : MonoBehaviour
{
        public GameObject[] gameObjects; // Array to hold your GameObjects
        private int currentIndex = 0; // To keep track of the current GameObject
        public Text textComponent;
        void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                if (currentIndex < gameObjects.Length)
                {
                    if (currentIndex > 0)
                    {
                        gameObjects[currentIndex - 1].SetActive(false); // Deactivate the previous GameObject
                    }
                    gameObjects[currentIndex].SetActive(true); // Activate the current GameObject
                    textComponent.text = currentIndex.ToString();
                    currentIndex++; // Move to the next GameObject
                }
            }
        }
    }
}