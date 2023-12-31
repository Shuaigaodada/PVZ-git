
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

public class NormalZombie : Zombie
{
	public float lostHeadHP;
	public GameObject head;
	private float SoundTimer = 0.5f;
	private float SoundPlaySpeed = 0.5f;
	[ReadOnly][SerializeField] private bool lostHead = false;
    // Start is called before the first frame update
    protected override void Start()
    {
		base.Start();
		if(lostHeadHP == 0f) 
			lostHeadHP = 75;
		if(head == null)
			head = transform.Find("Head").gameObject;
	}

    // Update is called once per frame
	public override float ChangeHealth(float damage)
	{
		float newHealth = base.ChangeHealth(damage);
		if (currentHP <= lostHeadHP && !lostHead)
		{
			lostHead = true;
			animator.SetBool("lostHead", true);
			head.SetActive(true);
		}
		return newHealth;
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(isDead) return;
		if(collision.tag == "plant")
		{
			isWalk = false;
			animator.SetBool("walk", false);
		}
	}
	private void OnTriggerStay2D(Collider2D collision)
	{
		if(isDead) return;
		if(collision.tag == "plant")
		{
			if((SoundTimer += Time.deltaTime) >= SoundPlaySpeed)
			{
				SoundTimer = 0;
				SoundManager.Instance.PlaySound(Globals.S_ZombieEat, 0.5f);
			}
			if ((damageTimer = damageTimer + Time.deltaTime) >= damageInterval)
			{
				damageTimer = 0;
				if(collision.GetComponent<Plant>().ChangeHealth(-damage) <= 0)
				{
					isWalk = true;
					animator.SetBool("walk", true);
				}
			}
		}
	}
	private void OnTriggerExit2D(Collider2D collision)
	{
		if(isDead) return;
		if(collision.tag == "plant")
		{
			isWalk = true;
			animator.SetBool("walk", true);
		}
	}
	protected override void Update()
	{
		if(isDead) { StopAllCoroutines(); }
		base.Update();
	}
}
