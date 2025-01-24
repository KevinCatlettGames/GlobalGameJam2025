using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SpellTester : MonoBehaviour
{
    [SerializeField] private SO_Spell spell;

    private void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            spell.CastSpell(transform.position, transform.forward);
        }
    }
}
