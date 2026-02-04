using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
public class CharacterSwitcher : MonoBehaviour
{
    [SerializeField] Button switchButton;
    [SerializeField] CinemachineVirtualCamera cineCamera;
    [SerializeField] List<GameObject> characters;

    int currentIndex = 0;

    void Start()
    {
        switchButton.onClick.AddListener(SwitchCharacter);

        // Disable all first
        foreach (var c in characters)
            c.SetActive(false);

        // Enable first character
        ActivateCharacter(currentIndex);
    }

    void SwitchCharacter()
    {
        // Disable current
        characters[currentIndex].SetActive(false);
        if(characters[currentIndex].gameObject.transform.parent != null)
        {
            characters[currentIndex].gameObject.transform.parent.gameObject.SetActive(false);
        }

        // Move to next
        currentIndex = (currentIndex + 1) % characters.Count;

        // Enable next
        ActivateCharacter(currentIndex);
    }

    void ActivateCharacter(int index)
    {
        characters[index].SetActive(true);
        if(characters[index].gameObject.transform.parent != null)
        {
            characters[index].gameObject.transform.parent.gameObject.SetActive(true);
        }
        cineCamera.Follow = characters[index].transform;
        cineCamera.LookAt = characters[index].transform;
    }
}

