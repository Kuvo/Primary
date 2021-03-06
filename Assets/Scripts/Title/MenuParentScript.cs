﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 戻り値にfalseを返せば現在のメニューを閉じることが出来ます
/// </summary>
/// <param name="itemName">選択された項目名</param>
/// <returns></returns>
public delegate bool MenuSelectEventHandrer(string itemName);

public class MenuParentScript : MyMonoBehaviour
{
	public enum Type { UpDown, LeftRight }
	public enum Direction { None, Up, Down, Left, Right }
	/// <summary>
	/// 項目が決定された時に実行されるイベントハンドラ。falseを返すとメニューを閉じる
	/// </summary>
	public event MenuSelectEventHandrer MenuSelected;
	[SerializeField]
	private Camera menuCamera = null;
	private Vector3 oldMousePosition;

	protected override void Awake()
	{
		base.Awake();

		Items = transform.GetComponentsInChildren<MenuItemScript>();
		ItemCount = transform.childCount;

		if (SelecterPrefab == null)
		{
			Debug.Log("セレクターのプレハブが設定されていません");
		}
		else
		{
			Selecter = (GameObject)Instantiate(SelecterPrefab);
		}
	}

	void Start()
	{
		if (Selecter != null)
		{
			Selecter.transform.parent = IndexToItem(CurrentCursorIndex).transform;
			Selecter.transform.localPosition = Vector3.zero;
		}
	}

	protected override void Update()
	{
		if (SceneChangeSingleton.instance.IsFading)
		{
			return;
		}

		if (IsPopChildMenu)
		{
			if (ChildMenu != null)
			{
				// 子メニューが開いてるため、このメニューは操作を受け付けない
				return;
			}

			ReturnChildMenu();
		}

		Direction direction = Direction.None;

		if (Input.GetButton("CursorRight"))
		{
			direction = Direction.Right;
		}
		else if (Input.GetButton("CursorLeft"))
		{
			direction = Direction.Left;
		}
		else if (Input.GetButton("CursorUp"))
		{
			direction = Direction.Up;
		}
		else if (Input.GetButton("CursorDown"))
		{
			direction = Direction.Down;
		}

		if (direction != Direction.None)
		{
			if (!IsOldButton)
			{
				// 押した瞬間
				switch (direction)
				{
					case Direction.Up:
						CurrentCursorIndex = IndexToItem(CurrentCursorIndex).UpItemIndex;
						break;
					case Direction.Down:
						CurrentCursorIndex = IndexToItem(CurrentCursorIndex).DownItemIndex;
						break;
					case Direction.Left:
						CurrentCursorIndex = IndexToItem(CurrentCursorIndex).LeftItemIndex;
						break;
					case Direction.Right:
						CurrentCursorIndex = IndexToItem(CurrentCursorIndex).RightItemIndex;
						break;
				}
				IsOldButton = true;
			}
		}
		else
		{
			IsOldButton = false;
		}

		//------ 項目選択処理 ------
		bool isMenuDecision = false; // メニュー項目を決定

		// マウス座標の位置にあるゲームオブジェクトを得る
		Ray ray;
		Vector3 mousePosition = Input.mousePosition;
		if (menuCamera != null)
		{
			ray = menuCamera.ScreenPointToRay(mousePosition);
		}
		else
		{
			ray = Camera.main.ScreenPointToRay(mousePosition);
		}

		Debug.DrawRay(ray.origin, ray.direction * 1000.0f);

		RaycastHit hit;
		MenuItemScript menuItem = null;
		bool isMenuitem = false;
		// マウスがメニューの項目上にあるかどうか
		if (Physics.Raycast(ray, out hit, 1000.0f, 1 << Layers.Menu.layer))
		{
			// MenuItemScriptがアタッチされているのでメニュー項目
			menuItem = hit.transform.GetComponent<MenuItemScript>();
			isMenuitem = true;
			if (mousePosition != oldMousePosition && menuItem != null)
			{
				CurrentCursorIndex = menuItem.Index;
			}
		}
		oldMousePosition = mousePosition;

		if (!isMenuDecision && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyUp(KeyCode.Return)))
		{
			isMenuDecision = true;
		}

		if (Input.GetButtonDown("Select") && isMenuitem)
		{
			isMenuDecision = true;
		}

		if (isMenuDecision)
		{
			if (CurrentCursorItem.ChildMenuPrefab != null)
			{
				PopChildMenu();
			}
			else if (MenuSelected != null)
			{
				if (!MenuSelected(CurrentCursorName))
				{
					Destroy(gameObject);
				}
			}
		}

		if (Input.GetButtonDown(CanselMessage))
		{
			if (transform.parent == null)
			{
				if (!MenuSelected(CanselMessage))
				{
					Destroy(gameObject);
				}
			}
			else
			{
				if (!MenuSelected(ReturnMessage))
				{
					Destroy(gameObject);
				}
			}
		}
	}

	/// <summary>
	/// 子メニューを開く
	/// </summary>
	private void PopChildMenu()
	{
		if (NextSceneName != "")
		{
			SceneChangeSingleton.instance.LoadLevel(NextSceneName);
			return;
		}
		IsPopChildMenu = true;
		GameObject go = Instantiate(CurrentCursorItem.ChildMenuPrefab, CurrentCursorItem.transform.position + PopChildOffset, Quaternion.identity) as GameObject;
		ChildMenu = go.GetComponent<MenuParentScript>();
		ChildMenu.transform.parent = CurrentCursorItem.transform;
		ChildMenu.MenuSelected += MenuSelected;

		foreach (MenuItemScript item in Items)
		{
			SpriteRenderer itemSpriteRenderer = item.GetComponent<SpriteRenderer>();
			if (itemSpriteRenderer != null)
			{
				itemSpriteRenderer.sortingLayerName = "BackGround";
				itemSpriteRenderer.color = BackColor;
			}
		}

		Selecter.GetComponentInChildren<Renderer>().enabled = false;
	}

	/// <summary>
	/// 子メニューから復帰
	/// </summary>
	private void ReturnChildMenu()
	{
		IsPopChildMenu = false;

		foreach (MenuItemScript item in Items)
		{
			SpriteRenderer itemSpriteRenderer = item.GetComponent<SpriteRenderer>();
			if (itemSpriteRenderer != null)
			{
				itemSpriteRenderer.sortingLayerName = "ForeGround";
				itemSpriteRenderer.color = DefaultColor;
			}
		}

		Selecter.GetComponentInChildren<Renderer>().enabled = true;
	}

	// 項目番号からMenuItemを取得
	public MenuItemScript IndexToItem(int index)
	{
		foreach (var item in Items)
		{
			if (item.Index == index)
			{
				return item;
			}
		}
		return null;
	}

	// 項目名からMenuItemを取得
	public MenuItemScript NameToItem(string name)
	{
		foreach (var item in Items)
		{
			if (item.Name == name)
			{
				return item;
			}
		}

		return null;
	}

	// 現在のカーソル位置ID
	public int CurrentCursorIndex
	{
		get { return this.currentCursorIndex; }
		set
		{
			int index = value;
			// 範囲外なら変更しない
			if (index >= ItemCount || index < 0)
			{
				index = CurrentCursorIndex;
			}

			this.currentCursorIndex = index;
			Selecter.transform.parent = IndexToItem(this.currentCursorIndex).transform;
			Selecter.transform.localPosition = Vector3.zero;
		}
	}

	// 現在のカーソル位置ID
	public string CurrentCursorName
	{
		get
		{
			currentCursorName = IndexToItem(CurrentCursorIndex).GetComponent<MenuItemScript>().Name;
			return this.currentCursorName;
		}
		set
		{
			CurrentCursorIndex = NameToItem(value).GetComponent<MenuItemScript>().Index;
		}
	}

	public MenuItemScript[] GetMenuItems()
	{
		return Items;
	}

	public MenuItemScript CurrentCursorItem
	{
		get
		{
			return IndexToItem(CurrentCursorIndex);
		}
	}
	//[SerializeField]
	//    private Type ControllType = Type.UpDown;

	//private MenuItemScript currentCursorItem; // カーソル位置のMenuItem
	private int currentCursorIndex; // 現在カーソルが指しているメニュー項目
	private string currentCursorName; // 現在カーソルが指しているメニュー項目名

	private int ItemCount; // メニュー項目数
	private MenuItemScript[] Items;

	[SerializeField]
	private GameObject SelecterPrefab = null; // セレクタープレハブ 
	private GameObject Selecter; // セレクター

	private bool IsOldButton = false; // 以前上下ボタンが押されていたか

	private bool IsPopChildMenu = false; // 子メニューが開いているか
	private MenuParentScript ChildMenu = null; // 子メニュー
	private readonly Vector3 PopChildOffset = new Vector3(1f, -0.5f, 0f); // 子メニューが出現する位置調整

	public const string ReturnMessage = "ReturnParent"; // キャンセル時、親メニューがあるときにイベントハンドラに対して送られるメッセージ
	public const string CanselMessage = "Cancel"; // キャンセル時、親が無い(一番上の階層に戻った)時にイベントハンドラに対して送られるメッセージ

	private Color DefaultColor = new Color(1f, 1f, 1f);
	private Color BackColor = new Color(0.5f, 0.5f, 0.5f); // 後ろに隠れたメニュー用

	public string NextSceneName = "";
}
