namespace z1.Actors;

internal sealed class FoodActor : Actor
{
    // The original creates food in state $80 with a $FF timer, then advances the state on each
    // expiry until state $83 deactivates it. That's three $FF periods, $2FD frames in total.
    private const int PeriodCount = 3;

    private int _periods = PeriodCount;

    public FoodActor(Game game, int x, int y) : base(game, ObjType.Food, x, y)
    {
        Decoration = 0;
        ObjTimer = 0xFF;
    }

    public override void Update()
    {
        if (ObjTimer == 0)
        {
            _periods--;
            if (_periods == 0)
            {
                Delete();
                return;
            }
            ObjTimer = 0xFF;
        }

        // This is how food attracts some monsters.
        var roomObjId = Game.World.RoomObj?.ObjType ?? ObjType.None;

        // JOE: TODO: Wire up to actor.IsAttrackedToMeat

        if (roomObjId >= ObjType.BlueMoblin && roomObjId <= ObjType.BlueFastOctorock
            || roomObjId == ObjType.Vire
            || roomObjId == ObjType.BlueKeese
            || roomObjId == ObjType.RedKeese)
        {
            Game.World.SetObservedPlayerPos(X, Y);
        }
    }

    public override void Draw()
    {
        GlobalFunctions.DrawItemWide(Game, ItemId.Food, X, Y);
    }
}