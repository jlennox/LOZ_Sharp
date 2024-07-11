﻿using z1.Actors;

namespace z1;

internal partial class World
{
    void GetWorldCoord(int roomId, out int row, out int col )
    {
        row = (roomId & 0xF0) >> 4;
        col = roomId & 0x0F;
    }

    int MakeRoomId(int row, int col)
    {
        return (row << 4) | col;
    }

    int GetNextRoomId(int curRoomId, Direction dir)
    {
        GetWorldCoord(curRoomId, out var row, out var col);

        switch (dir)
        {
            case Direction.Left:
                if (col == 0)
                    return curRoomId;
                col--;
                break;

            case Direction.Right:
                if (col == WorldWidth - 1)
                    return curRoomId;
                col++;
                break;

            case Direction.Up:
                if (row == 0)
                    return curRoomId;
                row--;
                break;

            case Direction.Down:
                if (row == WorldHeight - 1)
                    return curRoomId;
                row++;
                break;
        }

        int nextRoomId = MakeRoomId(row, col);
        return nextRoomId;
    }

    void GetRoomCoord(int position, out int row, out int col )
    {
        row = position & 0x0F;
        col = (position & 0xF0) >> 4;
        row -= 4;
    }

    void GetRSpotCoord(int position, out int x, out int y )
    {
        x = (position & 0x0F) << 4;
        y = (position & 0xF0) | 0xD;
    }

    Point GetRoomItemPosition(byte position)
    {
        return new(position & 0xF0, (byte)(position << 4));
    }

    int GetDoorStateFace(DoorType type, bool state)
    {
        if (state)
            return doorFaces[(int)type].Open;
        return doorFaces[(int)type].Closed;
    }

    void ClearScreen()
    {
        // TODO: al_clear_to_color(al_map_rgb(0, 0, 0));
    }

    void ClearScreen(int sysColor)
    {
        // TODO: ALLEGRO_COLOR color = Graphics.GetSystemColor(sysColor);
        // TODO: al_clear_to_color(color);
    }

    void ClearDeadObjectQueue()
    {
        for (int i = 0; i < objectsToDeleteCount; i++)
        {
            objectsToDelete[i] = null;
        }

        objectsToDeleteCount = 0;
    }

    void SetOnlyObject(ObjectSlot slot, Actor obj)
    {
        // TODO assert(slot >= 0 && slot < (int)ObjectSlot.MaxObjects);
        if (objects[(int)slot] != null)
        {
            if (objectsToDeleteCount == (int)ObjectSlot.MaxObjects)
                ClearDeadObjectQueue();
            objectsToDelete[objectsToDeleteCount] = objects[(int)slot];
            objectsToDeleteCount++;
        }
        objects[(int)slot] = obj;
    }

    LadderActor GetLadderObj()
    {
        return (LadderActor)GetObject(ObjectSlot.Ladder);
    }

    void SetLadderObj(LadderActor ladder)
    {
        SetOnlyObject(ObjectSlot.Ladder, ladder);
    }

    void SetBlockObj(Actor block)
    {
        SetOnlyObject(ObjectSlot.Block, block);
    }

    void DeleteObjects()
    {
        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            objects[i] = null;
        }

        ClearDeadObjectQueue();
    }

    void CleanUpRoomItems()
    {
        DeleteObjects();
        SetItem(ItemSlot.Clock, 0);
    }

    void DeleteDeadObjects()
    {
        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            Actor? obj = objects[i];
            if (obj != null && obj.IsDeleted)
            {
                objects[i] = null;
            }
        }

        ClearDeadObjectQueue();
    }

    void InitObjectTimers()
    {
        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            objectTimers[i] = 0;
        }
    }

    void DecrementObjectTimers()
    {
        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            if (objectTimers[i] != 0)
                objectTimers[i]--;

            objects[i]?.DecrementObjectTimer();
        }

        // ORIGINAL: Here the player isn't part of the array, but in the original it's the first element.
        Game.Link.DecrementObjectTimer();
    }

    void InitStunTimers()
    {
        longTimer = 0;
        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            stunTimers[i] = 0;
        }
    }

    void DecrementStunTimers()
    {
        if (longTimer > 0)
        {
            longTimer--;
            return;
        }

        longTimer = 9;

        for (int i = 0; i < (int)ObjectSlot.MaxObjects; i++)
        {
            if (stunTimers[i] != 0)
                stunTimers[i]--;

            objects[i]?.DecrementStunTimer();
        }

        // ORIGINAL: Here the player isn't part of the array, but in the original it's the first element.
        Game.Link.DecrementStunTimer();
    }

    void InitPlaceholderTypes()
    {
        Array.Clear(placeholderTypes);
    }

    ObjectSlot FindEmptyMonsterSlot()
    {
        for (int i = (int)ObjectSlot.LastMonster; i >= 0; i--)
        {
            if (objects[i] == null)
                return (ObjectSlot)i;
        }
        return ObjectSlot.NoneFound;
    }

    void ClearRoomItemData()
    {
        recorderUsed = 0;
        candleUsed = false;
        summonedWhirlwind = false;
        shuttersPassedDirs = Direction.None;
        brightenRoom = false;
        activeShots = 0;
    }

    void SetPlayerColor()
    {
        static ReadOnlySpan<byte> palette() => new byte[] { 0x29, 0x32, 0x16 };

        int value = profile.Items[ItemSlot.Ring];

        Graphics.SetColorIndexed(Palette.Player, 1, palette()[value]);
    }

    void SetFlashPalette()
    {
        static ReadOnlySpan<byte> palette() => new byte[] { 0x0F, 0x30, 0x30, 0x30 };

        for (int i = 2; i < Global.BackgroundPalCount; i++)
        {
            Graphics.SetPaletteIndexed((Palette)i, palette());
        }

        Graphics.UpdatePalettes();
    }

    void SetLevelPalettes(byte[][] palettes) // const byte palettes[2][PaletteLength] )
    {
        for (int i = 0; i< 2; i++ )
        {
            Graphics.SetPaletteIndexed((Palette)2 + i, palettes[i]);
        }

        Graphics.UpdatePalettes();
    }

    void SetLevelPalette()
    {
        var infoBlock = GetLevelInfo();

        for (int i = 2; i < Global.BackgroundPalCount; i++)
        {
            Graphics.SetPaletteIndexed((Palette)i, infoBlock.GetPalette(i));
        }

        Graphics.UpdatePalettes();
    }

    void SetLevelFgPalette()
    {
        var infoBlock = GetLevelInfo();
        Graphics.SetPaletteIndexed(Palette.LevelFgPalette, infoBlock.GetPalette((int)Palette.LevelFgPalette));
    }
}