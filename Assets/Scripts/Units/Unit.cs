using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Unit : Selectable
{

	/// Basic stats
	protected int maxMoveRange;

	protected int health;
	public int Health
	{
		get => this.health;
	}

	protected int maxHealth;
	public int MaxHealth
	{
		get => this.maxHealth;
	}

	protected int closeDefense;
	protected int longDefense;
	protected int closeAttack;
	protected int longAttack;
	protected int attackRange;
	protected bool baseStatsSet;

    [SerializeField] protected bool moving = false;
    [SerializeField] protected List<Vector3> targetPath;
    [SerializeField] protected int currentPathIndex;
    [SerializeField] protected List<GridCell> cellPath;
    [SerializeField] protected int movesMade;
	protected bool hasAttacked;

    /// When not active, maybe it could display a faded out
	/// unit in the game - active state mostly applies to certain units
	/// ex - refineries
	public virtual bool IsActive { get => true; }

	public UnitType unitType;

    public GridCell target;

	[SerializeField] protected MonoBehaviourGameEvent onUnitDestroyed;

	[SerializeField] protected HeavyGameEvent onUnitAttack;

	/// attribute to allow editing the faction for debugging
	[SerializeField] protected Faction faction;
	public Faction Faction { get => this.faction; }

	/// for quick debugging
	[SerializeField] protected bool debug = false;

	[SerializeField] public int movesLeft = 0;

	public bool isPlayerUnit;

	[SerializeField] AnimationCurve movementInterpolationCurve;
	[SerializeField] GameObject explosionPrefab;

	private void Awake()
	{
		this.baseStatsSet = false;
	}

	private void Start()
	{
		if(!this.baseStatsSet)
		{
			throw new System.Exception("Unit was instantiated, but no base stats were set. Make sure you are instantiating through UnitInfo");
		}
	}

    private void Update()
    {
        if (this.moving)
        {
            this.HandleMovement();
        }
    }

	public virtual void SetBaseStats(string name, int maxMoveRange, int health, int closeDefense,
			int longDefense, int closeAttack, int longAttack, int attackRange, string extra, Faction _faction) {

		gameObject.name = name;
		this.maxMoveRange = maxMoveRange;
		this.health = health;
		this.closeDefense = closeDefense;
		this.longDefense = longDefense;
		this.closeAttack = closeAttack;
		this.longAttack = longAttack;
		this.attackRange = attackRange;

		this.baseStatsSet = true;

		this.faction = _faction;

		this.setExtra(extra);
	}

	/// Further initialization based on the extra parameter
	/// Useful for subchildren of the Unit class
	protected virtual void setExtra(string extra)
	{
	}

	/// Attached to the NextTurn IntProperty
	/// Updates / Resets the turn fields for the Unit to get ready for the turn
	public virtual void OnStartFactionTurn(Faction faction)
	{
		if(faction == this.Faction)
		{
			this.updatePreTurn();
		}
	}

	/// Called at the start of the turn
	protected virtual void updatePreTurn()
	{
		this.movesLeft = this.maxMoveRange;
		this.hasAttacked = false;
        this.movesMade = 0;
	}

	/// Connected by a HeavyGameEventListener
	/// Initial check to see if the fired HeavyGameEventData has to do with this unit
	public virtual void OnFilterReceiveGameEvent(HeavyGameEventData data)
	{
		if(data.TargetCell == this.ParentCell)
		{
			this.receiveAction(data);
		}
	}

	/// Returns whether or not the action can be performed
	public override bool CanPerformAction(SelectableActionType actionType, GridCell targetCell, string param)
	{
		if (targetCell != null && actionType != SelectableActionType.None)
		{
			if(this.GetValidActionTypes().Contains(actionType))
			{
				switch(actionType)
				{
					case SelectableActionType.Attack:
						/// Attacks if the targetCell has a selectable and if the Faction belonging to the selectable is
						/// not the current faction
						return targetCell.Selectable is Unit && ((Unit)targetCell.Selectable).Faction != this.Faction
						&& !this.hasAttacked &&
						ParentCell.parentGrid.GetCellsInRange(ParentCell, this.attackRange).Contains(targetCell);
					case SelectableActionType.Move:
						/// TODO: Pathfinder now handles movement to non-empty cells, what do we do here?
						return ParentCell.parentGrid.pathfinder.FindPath(ParentCell.layer, ParentCell.slice, targetCell.layer, targetCell.slice) != null;
				}
			}
		}
		return false;
	}

	public override void PerformAction(SelectableActionType actionType, GridCell targetCell, string param)
	{
		switch(actionType)
		{
			case SelectableActionType.Attack:
				this.performAttack(targetCell);
			break;
			case SelectableActionType.Move:
				this.performMove(targetCell);
			break;
		}
	}

	/// Returns list of valid actions based on the current turn state
	/// This is based on the local client
	public override List<SelectableActionType> GetValidActionTypes()
	{
		List<SelectableActionType> actionTypes = new List<SelectableActionType>(this.validActionTypes);
		if(this.movesLeft <= 0)
		{
			actionTypes.Remove(SelectableActionType.Move);
		}
		if (this.hasAttacked)
		{
			actionTypes.Remove(SelectableActionType.Attack);
		}
		return actionTypes;
	}

	/// processes performed action
	// This may be obsoleted by more specific events and as things
	// get moved into managers (which I'm a big fan of actually) - Jeff 4/17/2020
	public virtual void receiveAction(HeavyGameEventData data)
	{
	}

	/// Raises an attack action
	protected virtual void performAttack(GridCell targetCell)
	{
		HeavyGameEventData data = new HeavyGameEventData();
		data.SourceCell = this.ParentCell;
		data.TargetCell = targetCell;
		data.ActionType = SelectableActionType.Attack;
		onUnitAttack.Raise(data);
		this.hasAttacked = true;
	}

	/// Raises a move action
    public virtual void performMove(GridCell targetIn)
    {
        this.cellPath = ParentCell.parentGrid.pathfinder.FindPath(ParentCell.layer, ParentCell.slice, targetIn.layer, targetIn.slice);
        this.targetPath = ParentCell.parentGrid.pathfinder.FindVectorPath(cellPath);
        this.target = cellPath[cellPath.Count - 1];
        HeavyGameEventData data = new HeavyGameEventData();
		data.SourceCell = this.ParentCell;
		data.TargetCell = cellPath[cellPath.Count - 1];
        this.currentPathIndex = 0;
        this.moving = true;
        GameStateManager.Instance.AnimationPresent = true;
        GameStateManager.Instance.PerformAction(data);
    }

    /// Should be implemented in base Unit class for
    /// leveling up / upgrading Units
    protected virtual void performEnhance(string param)
    {}

    public override void SetParentCell(GridCell cellIn)
    {
        this.ParentCell = cellIn;
		cellIn.Selectable = this;
    }

	public void SetForceMove(GridCell sourceCell, GridCell targetCell)
	{
		this.ParentCell = sourceCell;
		this.movesLeft++;
		this.movesMade--;
		this.performMove(targetCell);
	}

    public GridCell GetParentCell()
    {
        return this.ParentCell;
    }

    public int GetMaxRange()
    {
        return maxMoveRange;
    }

	public (int closeAttack, int longAttack) GetAttack()
    {
        return (this.closeAttack, longAttack);
    }

	public int GetAttackRange()
	{
		return attackRange;
	}

	public (int closeDefense, int longDefense) GetDefense()
    {
        return (this.closeDefense, longDefense);
    }

	public void TakeDamage(int dmg)
	{
		if(dmg >= 0)
		{
			this.health -= dmg;

			foreach(Transform child in this.transform) {
				if(child.GetComponent<Animator>()) {
					child.GetComponent<Animator>().Play("ship damage",  -1, 0f);
				}
			}

		}
		if(this.health <= 0)
		{
			Instantiate(explosionPrefab,transform.position,transform.rotation);
			onUnitDestroyed.Raise(this);
		}
	}

	//InverseLerp with Vector3 for the movement interpolation.
	public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
	{
		Vector3 AB = b - a;
		Vector3 AV = value - a;
		return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
	}

    private void HandleMovement()
    {
		if(this.movesMade < this.maxMoveRange)
		{
			if (this.targetPath != null && this.currentPathIndex < this.targetPath.Count)
			{
				Vector3 targetPosition = this.targetPath[this.currentPathIndex];
				float step = Mathf.Pow(30 * Time.deltaTime,2);
				if ((this.transform.position - targetPosition).sqrMagnitude > step)
				{
					Vector3 endMovement = this.targetPath[currentPathIndex];
					transform.LookAt(endMovement);

					Vector3 moveDir = (targetPosition - this.transform.position).normalized;
					this.transform.position = this.transform.position + moveDir * 50.0f * Time.deltaTime;
				}
				else
				{
					this.currentPathIndex++;
					if(this.currentPathIndex > 1)
					{
						this.movesMade++;
						this.movesLeft--;
					}
					if (this.currentPathIndex == this.targetPath.Count)
					{
						this.endMove();
						this.Faction.OnEndMove();
					}
					else if (movesMade == maxMoveRange){
						this.endMoveInMiddle();
						this.Faction.OnEndMove();
					}
				}
			}
		}
    }

    private void endMove()
    {
		HeavyGameEventData data = new HeavyGameEventData();
        data.SourceCell = this.ParentCell;
        data.TargetCell = this.target;
		data.targetSelectable = this;
        this.moving = false;
        this.currentPathIndex = 1;
        this.target = null;

        GameStateManager.Instance.AnimationPresent = false;
        this.onMoveEvent.Raise(data);
    }

    private void endMoveInMiddle()
    {
		HeavyGameEventData data = new HeavyGameEventData();
        data.SourceCell = this.ParentCell;
        data.TargetCell = this.cellPath[this.currentPathIndex-1];
        this.onMoveEvent.Raise(data);
        this.moving = false;
        this.targetPath = this.targetPath.Skip(this.currentPathIndex-1).ToList();
        this.cellPath = this.cellPath.Skip(this.currentPathIndex-1).ToList();

        GameStateManager.Instance.AnimationPresent = false;
    }

    public bool TakeRemainingMoves()
    {
		if (this.movesMade < this.maxMoveRange)
		{
			if(this.target != null)
			{
				GameStateManager.Instance.AnimationPresent = false;
				if (this.CanPerformAction(SelectableActionType.Move, this.target, null))
				{
					this.performMove(this.target);
				}
				else {
					this.target = null;
					this.cellPath = null;
					this.targetPath = null;
					this.currentPathIndex = 0;
				}
				return true;
			}
		}
		return false;
    }
}
public enum UnitType
{
	Base,
	Builder,
	Mine,
	Refinery,
	Wall,
	Attacker,
	Defender
}