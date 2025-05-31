using System;
using System.Collections.Concurrent;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.Draw;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Utility.Numerics;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KodakkuAssist.Data;
using KodakkuAssist.Module.GameOperate;
using Lumina.Excel.Sheets;
using Newtonsoft.Json.Linq;
using KodakkuAssist.Module.Draw.Manager;

namespace CicerosKodakkuAssist.FuturesRewrittenUltimate;
[ScriptType(name: "神兵三连桶",
    territorys: [777],
    guid: "c32af9b6-d9ad-45cc-ab59-260b3b350090",
    version: "0.0.1",
    note: notesOfTheScript,
    author: "嗨呀",
    updateInfo: UpdateInfo)]
public class 神兵三连桶
{
    private const string UpdateInfo =
        """
        .01 暂时只有三连桶
        """;



    const string notesOfTheScript =
        """
        ***** Please read the note here carefully before running the script! *****
        ***** 请在使用此脚本前仔细阅读此处的说明! *****
        
        """;
    #region User_Settings_用户设置
    [UserSetting("是否开启三连桶标记")]
    public bool EnableMark { get; set; }= true;
    
    #endregion User_Settings_用户设置
    private readonly object _is三连桶Lock = new();
    bool endOf三连桶 = false;
    List<int> markIndex = new ();
    bool is三连桶 = false;
    int? firstTargetIcon = null;
    Vector3 bossPosition = new ();
    List<int> targetIndex = new();//三连桶点名记录
    public List<int> customOrder = new List<int> { 0, 1, 4, 5, 6, 7, 2, 3 };//三连桶顺序 mt st d1234 h12
    #region 脚本初始化

    public void Init(ScriptAccessory accessory)
    {
        if(EnableMark)accessory.Method.MarkClear();
        is三连桶 = false;
        bossPosition = new Vector3(100,0,100);
        targetIndex.Clear();
        markIndex.Clear();
        accessory.Method.RemoveDraw("");
        endOf三连桶 = false;
    }

    #endregion 脚本初始化

    [ScriptMethod(name: "三连桶标记", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(11116|11115)$"], userControl:false)]
    public void 三连桶标记(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var tid)) return;
        var index = accessory.Data.PartyList.IndexOf((uint)tid);
        targetIndex.Add(index);
        if (targetIndex.Count == 3)
        {
            markIndex = targetIndex.OrderBy(i => customOrder.IndexOf(i)).ToList();
            accessory.Method.SendChat("/e 点名记录完成");
            if(!EnableMark) return;
            accessory.Method.Mark(accessory.Data.PartyList[markIndex[0]], MarkType.Attack1, false); //给顺序1的人标1
            accessory.Method.Mark(accessory.Data.PartyList[markIndex[1]], MarkType.Attack2, false); //给顺序2的人标2
            accessory.Method.Mark(accessory.Data.PartyList[markIndex[2]], MarkType.Attack3, false); //给顺序3的人标3
        }
    }

    [ScriptMethod(name: "boss位置记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(11111)$"], userControl: false)]
    public void boss位置记录(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sid)) return;
        bossPosition = accessory.GetById(sid)?.Position??new Vector3(100,0,100);
        //accessory.Method.SendChat($"/e boss位置记录完成{bossPosition}");
    }
    [ScriptMethod(name: "三连桶开始标识", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(11114)$"], userControl: false)]
    public void 三连桶开始标识(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sid)) return;
        lock (_is三连桶Lock)
        {
            if(is三连桶)return;
            accessory.Method.SendChat("/e 三连桶开始");
            is三连桶 = true;
        }
    }
    
    [ScriptMethod(name: "三连桶点名范围测试", eventType:EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(11116|11115)$"],userControl: false)]
    public void 花岗岩石牢(Event @event, ScriptAccessory accessory)
    {
        if(endOf三连桶)return;
        if (!ParseObjectId(@event["TargetId"], out var tid)) return;
        var dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "点名范围测试";
        dp.Scale = new(6);
        dp.DestoryAt = 7000;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Owner = tid;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "hitbox范围测试";
        dp.Scale = new(1.5f);
        dp.DestoryAt = 7000;
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.Owner = tid;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

    /*[ScriptMethod(name: "爆破岩石ToCenter", eventType: EventTypeEnum.AddCombatant, userControl: false)]
    public void 爆破岩石ToCenter(Event @event, ScriptAccessory accessory)
    {
        if (!is三连桶) return;
        if(endOf三连桶)return;
        if(@event["SourceName"]!= "爆破岩石") return;
        if(!ParseObjectId(@event["SourceId"], out var sid)) return;
        var sPosition = ParseVector3(@event["SourcePosition"]);
        var radius = sPosition.FindRadianDifference(bossPosition, new Vector3(100, 0, 100));
        accessory.Method.SendChat($"/e 角度{radius}");
        var radiusPosition = sPosition.RotatePoint(new Vector3(100, 0, 100), (float)(Math.PI-radius));
        
        var dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "爆破岩石toCenter";
        dp.Scale = new(2);
        dp.DestoryAt = 50000;
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Owner = sid;
        dp.TargetPosition = new Vector3(100,0,100);
        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "bossToCenter";
        dp.Scale = new(2);
        dp.DestoryAt = 50000;
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Position = bossPosition;
        dp.TargetPosition = new Vector3(100,0,100);
        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "选择后位置";
        dp.Scale = new(2);
        dp.DestoryAt = 50000;
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Position = radiusPosition;
        dp.TargetPosition = new Vector3(100,0,100);
        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        
    }*/
    /*[ScriptMethod(name: "爆破岩石", eventType: EventTypeEnum.AddCombatant, userControl: false)]
    public void 爆破岩石(Event @event, ScriptAccessory accessory)
    {
        if (!is三连桶) return;
        if(endOf三连桶)return;
        if(@event["SourceName"] != "爆破岩石") return;
        if(!ParseObjectId(@event["SourceId"], out var sid)) return;
        endOf三连桶 = true;
        var sPosition = accessory.GetById(sid)?.Position??new Vector3(100,0,100);
        accessory.Method.SendChat($"/e 位置{sPosition}");
        var radius = CalculateSignedAngleRadian(bossPosition, new Vector3(100, 0, 100), sPosition);
        accessory.Method.SendChat($"/e 角度{radius}");
        sPosition = sPosition.RotatePoint(new Vector3(100, 0, 100), radius>0?MathF.PI-radius:-MathF.PI-radius);
        
        var dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "爆破岩石";
        dp.Scale = new(6);
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.DestoryAt = 50000;
        dp.Owner = sid;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "一桶";
        dp.Scale = new(6);
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.DestoryAt = 50000;
        dp.Position = sPosition.CalculatePointOnLine(bossPosition, -6 * 3);
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "二桶";
        dp.Scale = new(6);
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.DestoryAt = 50000;
        dp.Position = sPosition.CalculatePointOnLine(bossPosition, -6 * 2);
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        
        dp =accessory.Data.GetDefaultDrawProperties();
        dp.Name = "三桶";
        dp.Scale = new(6);
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.DestoryAt = 50000;
        dp.Position = sPosition.CalculatePointOnLine(bossPosition, -6 * 1);
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }*/
    [ScriptMethod(name: "三连桶指路", eventType: EventTypeEnum.AddCombatant, userControl: false)]
    public void 三连桶指路(Event @event, ScriptAccessory accessory)
    {
        if (!is三连桶) return;
        if(endOf三连桶)return;
        if(@event["SourceName"] != "爆破岩石") return;
        if(!ParseObjectId(@event["SourceId"], out var sid)) return;
        if(!targetIndex.Contains(accessory.Data.PartyList.IndexOf(accessory.Data.Me)))return;
        var sPosition = accessory.GetById(sid)?.Position??new Vector3(100,0,100);
        //accessory.Method.SendChat($"/e 位置{sPosition}");
        var radius = CalculateSignedAngleRadian(bossPosition, new Vector3(100, 0, 100), sPosition);
        //accessory.Method.SendChat($"/e 角度{radius}");
        sPosition = sPosition.RotatePoint(new Vector3(100, 0, 100), radius>0?MathF.PI-radius:-MathF.PI-radius);
        var goPosition = sPosition.CalculatePointOnLine(bossPosition, 
             -6 * (3-markIndex.IndexOf(accessory.Data.PartyList.IndexOf(accessory.Data.Me))));
        
        var dp =accessory.Data.GetDefaultDrawProperties();
        dp=accessory.Data.GetDefaultDrawProperties();
        dp.Name = "三连桶指路";
        dp.Scale = new(2);
        dp.DestoryAt = 5000;
        dp.Color = accessory.Data.DefaultSafeColor;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Owner = accessory.Data.Me;
        dp.TargetPosition = goPosition;
        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        endOf三连桶 = true;
    }
    
    #region Common_Mathematical_Wheels_常用数学轮子
    /// <summary>
    /// 计算从点A到点B相对于中心点的有向夹角（弧度，范围 [-π, π]）
    /// </summary>
    /// <returns>有向夹角（弧度）</returns>
    public static float CalculateSignedAngleRadian( Vector3 pointA,Vector3 center, Vector3 pointB)
    {
        // 计算向量A和向量B相对于中心点的坐标
        float dxA = pointA.X - center.X;
        float dyA = pointA.Z - center.Z;
        float dxB = pointB.X - center.X;
        float dyB = pointB.Z - center.Z;

        // 处理零向量情况（返回0弧度）
        if (dxA == 0 && dyA == 0 || dxB == 0 && dyB == 0)
            return 0;

        // 使用Atan2获取向量与x轴正方向的夹角（弧度，范围[-π, π]）
        float angleA = MathF.Atan2(dyA, dxA);
        float angleB = MathF.Atan2(dyB, dxB);

        // 计算角度差（从A到B的有向弧度）
        float deltaAngle = angleB - angleA;

        // 归一化到[-π, π]范围内（处理超过π或小于-π的情况）
        deltaAngle = NormalizeRadian(deltaAngle);

        return deltaAngle;
    }

    /// <summary>
    /// 将弧度归一化到[-π, π]范围内
    /// </summary>
    private static float NormalizeRadian(float radian)
    {
        // 取模运算，将弧度转换为[-π, 3π)范围内
        radian = radian % (2 * MathF.PI);
        
        // 调整到[-π, π]
        if (radian > MathF.PI)
            radian -= 2 * MathF.PI;
        else if (radian < -MathF.PI)
            radian += 2 * MathF.PI;
        
        return radian;
    }
    public static Vector3 ParseVector3(string jsonString)
    {
        // 匹配X、Y、Z的值
        var regex = new Regex(@"\""X\"":(\d+\.\d+),\""Y\"":(\d+\.\d+),\""Z\"":(\d+\.\d+)");
        var match = regex.Match(jsonString);
        
        if (match.Success && match.Groups.Count == 4)
        {
            float x = float.Parse(match.Groups[1].Value);
            float y = float.Parse(match.Groups[2].Value);
            float z = float.Parse(match.Groups[3].Value);
            return new Vector3(x, y, z);
        }
        
        return new Vector3(100,0,100);; // 解析失败时返回默认值
    }
    private async void TpToPosition(ScriptAccessory accessory, Vector3 pos, int delay)
    {
        if (delay > 0)
            await Task.Delay(delay);
        //accessory.Method.SendChat($"/aetp {pos.X:F1},{pos.Y:F1},{pos.Z:F1}");
        accessory.SetPostion(accessory.GetMe(), pos);
    }
    private int ParsTargetIcon(string id)
    {
        firstTargetIcon ??= int.Parse(id, System.Globalization.NumberStyles.HexNumber);
        return int.Parse(id, System.Globalization.NumberStyles.HexNumber) - (int)firstTargetIcon;
    }
    private static bool ParseObjectId(string? idStr, out ulong id)
    {
        id = 0;
        if (string.IsNullOrEmpty(idStr)) return false;
        try
        {
            var idStr2 = idStr.Replace("0x", "");
            id = ulong.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    /// <summary>
    /// 向近的取
    /// </summary>
    /// <param name="point"></param>
    /// <param name="centre"></param>
    /// <returns></returns>
    private int PositionTo8Dir(Vector3 point, Vector3 centre)
    {
        // Dirs: N = 0, NE = 1, ..., NW = 7
        var r = Math.Round(4 - 4 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 8;
        return (int)r;

    }
    private int PositionTo6Dir(Vector3 point, Vector3 centre)
    {
        var r = Math.Round(3 - 3 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 6;
        return (int)r;

    }
    private Vector3 RotatePoint(Vector3 point, Vector3 centre, float radian)
    {

        Vector2 v2 = new(point.X - centre.X, point.Z - centre.Z);

        var rot = (MathF.PI - MathF.Atan2(v2.X, v2.Y) + radian);
        var lenth = v2.Length();
        return new(centre.X + MathF.Sin(rot) * lenth, centre.Y, centre.Z - MathF.Cos(rot) * lenth);
    }

    #endregion Common_Mathematical_Wheels_常用数学轮子
    #region 类函数

    private class UlRelativity
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public ScriptAccessory sa { get; set; } = null!;
        public int RelativeNorth { get; set; } = -1;
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public PriorityDict pd { get; set; } = null!;
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public Counter ct { get; set; } = null!;
        public List<int> TetherDirection { get; set; } = Enumerable.Repeat(0, 8).ToList();  // 0无，1加速，2减速
        public List<int> RelativeDirection { get; set; } = [4, 6, 5, 3, 7, 1, 2, 0];
        public List<int> TrueDirection { get; set; } = Enumerable.Repeat(0, 8).ToList();
        public void Init(ScriptAccessory accessory, PriorityDict priorityDict, Counter counter)
        {
            sa = accessory;
            pd = priorityDict;
            ct = counter;
            TetherDirection = Enumerable.Repeat(0, 8).ToList();
            TrueDirection = Enumerable.Repeat(0, 8).ToList();
            RelativeNorth = -1;
        }

        public void BuildTrueDirection()
        {
            // 找到减速灯
            var slowTetherIdx = TetherDirection.IndexOf(2, 0);
            var checkIdx1 = (slowTetherIdx - 2 + 8) % 8;
            var checkIdx2 = (slowTetherIdx + 2 + 8) % 8;
            RelativeNorth = TetherDirection[checkIdx1] == 1 ? checkIdx1 : checkIdx2;
            sa.Log.Debug($"根据减速线{slowTetherIdx}计算相对北，检查{checkIdx1}与{checkIdx2}，得到{RelativeNorth}为相对北");

            for (int i = 0; i < 8; i++)
            {
                var jobIdx = pd.SelectSpecificPriorityIndex(i).Key;
                var dir = (RelativeDirection[i] + RelativeNorth) % 8;
                TrueDirection[jobIdx] = dir;
                sa.Log.Debug($"字典顺序{i}, {sa.GetPlayerJobByIndex(jobIdx)}({jobIdx}), 需去方位{dir}。");
            }
        }

        public void ShowMessage()
        {
            var str = "\n ---- [时间压缩] ----\n";
            str += $"相对北：{RelativeNorth}。\n";
            str += $"各职能位置：{sa.BuildListStr(TrueDirection)}";

            sa.Log.Debug(str);
        }

        public int GetDirection(int jobIdx)
        {
            return TrueDirection[jobIdx];
        }

    }

    public class PriorityDict
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public ScriptAccessory accessory { get; set; } = null!;
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public Dictionary<int, int> Priorities { get; set; } = null!;
        public string Annotation { get; set; } = "";
        public int ActionCount { get; set; } = 0;

        public void Init(ScriptAccessory _accessory, string annotation, int partyNum = 8)
        {
            accessory = _accessory;
            Priorities = new Dictionary<int, int>();
            for (var i = 0; i < partyNum; i++)
            {
                Priorities.Add(i, 0);
            }
            Annotation = annotation;
            ActionCount = 0;
        }

        /// <summary>
        /// 为特定Key增加优先级
        /// </summary>
        /// <param name="idx">key</param>
        /// <param name="priority">优先级数值</param>
        public void AddPriority(int idx, int priority)
        {
            Priorities[idx] += priority;
        }

        /// <summary>
        /// 从Priorities中找到前num个数值最小的，得到新的Dict返回
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public List<KeyValuePair<int, int>> SelectSmallPriorityIndices(int num)
        {
            return SelectMiddlePriorityIndices(0, num);
        }

        /// <summary>
        /// 从Priorities中找到前num个数值最大的，得到新的Dict返回
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public List<KeyValuePair<int, int>> SelectLargePriorityIndices(int num)
        {
            return SelectMiddlePriorityIndices(0, num, true);
        }

        /// <summary>
        /// 从Priorities中找到升序排列中间的数值，得到新的Dict返回
        /// </summary>
        /// <param name="skip">跳过skip个元素。若从第二个开始取，skip=1</param>
        /// <param name="num"></param>
        /// <param name="descending">降序排列，默认为false</param>
        /// <returns></returns>
        public List<KeyValuePair<int, int>> SelectMiddlePriorityIndices(int skip, int num, bool descending = false)
        {
            if (Priorities.Count < skip + num)
                return new List<KeyValuePair<int, int>>();

            IEnumerable<KeyValuePair<int, int>> sortedPriorities;
            if (descending)
            {
                // 根据值从大到小降序排序，并取前num个键
                sortedPriorities = Priorities
                    .OrderByDescending(pair => pair.Value) // 先根据值排列
                    .ThenBy(pair => pair.Key) // 再根据键排列
                    .Skip(skip) // 跳过前skip个元素
                    .Take(num); // 取前num个键值对
            }
            else
            {
                // 根据值从小到大升序排序，并取前num个键
                sortedPriorities = Priorities
                    .OrderBy(pair => pair.Value) // 先根据值排列
                    .ThenBy(pair => pair.Key) // 再根据键排列
                    .Skip(skip) // 跳过前skip个元素
                    .Take(num); // 取前num个键值对
            }

            return sortedPriorities.ToList();
        }

        /// <summary>
        /// 从Priorities中找到升序排列第idx位的数据，得到新的Dict返回
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="descending">降序排列，默认为false</param>
        /// <returns></returns>
        public KeyValuePair<int, int> SelectSpecificPriorityIndex(int idx, bool descending = false)
        {
            var sortedPriorities = SelectMiddlePriorityIndices(0, 8, descending);
            return sortedPriorities[idx];
        }

        /// <summary>
        /// 从Priorities中找到对应key的数据，得到其Value排序后位置返回
        /// </summary>
        /// <param name="key"></param>
        /// <param name="descending">降序排列，默认为false</param>
        /// <returns></returns>
        public int FindPriorityIndexOfKey(int key, bool descending = false)
        {
            var sortedPriorities = SelectMiddlePriorityIndices(0, 8, descending);
            var i = 0;
            foreach (var dict in sortedPriorities)
            {
                if (dict.Key == key) return i;
                i++;
            }

            return i;
        }

        /// <summary>
        /// 一次性增加优先级数值
        /// 通常适用于特殊优先级（如H-T-D-H）
        /// </summary>
        /// <param name="priorities"></param>
        public void AddPriorities(List<int> priorities)
        {
            if (Priorities.Count != priorities.Count)
                throw new ArgumentException("输入的列表与内部设置长度不同");

            for (var i = 0; i < Priorities.Count; i++)
                AddPriority(i, priorities[i]);
        }

        /// <summary>
        /// 输出优先级字典的Key与优先级
        /// </summary>
        /// <returns></returns>
        public string ShowPriorities()
        {
            var str = $"{Annotation} 优先级字典：\n";
            foreach (var pair in Priorities)
            {
                str += $"Key {pair.Key} ({accessory.GetPlayerJobByIndex(pair.Key)}), Value {pair.Value}\n";
            }
            return str;
        }

        public string PrintAnnotation()
        {
            return Annotation;
        }

        public PriorityDict DeepCopy()
        {
            return JsonConvert.DeserializeObject<PriorityDict>(JsonConvert.SerializeObject(this)) ?? new PriorityDict();
        }

        public void AddActionCount(int count = 1)
        {
            ActionCount += count;
        }

        public bool IsActionCountEqualTo(int times)
        {
            return ActionCount == times;
        }
    }

    private class Counter
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public ScriptAccessory accessory { get; set; } = null!;
        public int Number { get; set; } = 0;
        public bool Enable { get; set; } = true;
        public string Annotation = "";

        public void Init(ScriptAccessory _accessory, string annotation, bool enable = true)
        {
            accessory = _accessory;
            Number = 0;
            Enable = enable;
            Annotation = annotation;
        }

        public string ShowCounter()
        {
            var str = $"{Annotation} 计数器【{(Enable ? "使能" : "不使能")}】：{Number}\n";
            return str;
        }

        public void DisableCounter()
        {
            Enable = false;
            var str = $"禁止 {Annotation} 计数器的数值改变。\n";
        }

        public void EnableCounter()
        {
            Enable = true;
            var str = $"使能 {Annotation} 计数器的数值改变。\n";
        }

        public void AddCounter(int num = 1)
        {
            if (!Enable) return;
            Number += num;
        }

        public void TimesCounter(int num = 1)
        {
            if (!Enable) return;
            Number *= num;
        }
    }

    #endregion 类函数

}
#region 函数集

file class Vector3Deserializer
{
    public static Vector3 ParseVector3(string jsonString)
    {
        var coordinates = JsonConvert.DeserializeObject<Coordinates>(jsonString);
        return new Vector3((float)coordinates.X, (float)coordinates.Y, (float)coordinates.Z);
    }

    private class Coordinates
    {
        [JsonProperty("X")]
        public double X { get; set; }

        [JsonProperty("Y")]
        public double Y { get; set; }

        [JsonProperty("Z")]
        public double Z { get; set; }
    }
}
file static class EventExtensions
{
    private static bool ParseHexId(string? idStr, out uint id)
    {
        id = 0;
        if (string.IsNullOrEmpty(idStr)) return false;
        try
        {
            var idStr2 = idStr.Replace("0x", "");
            id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public static uint DurationMilliseconds(this Event @event)
    {
        return JsonConvert.DeserializeObject<uint>(@event["DurationMilliseconds"]);
    }

    public static uint Id2(this Event @event)
    {
        return ParseHexId(@event["Id2"], out var id) ? id : 0;
    }

    public static uint Id0(this Event @event)
    {
        return ParseHexId(@event["Id"], out var id) ? id : 0;
    }

    public static uint TargetIndex(this Event @event)
    {
        return JsonConvert.DeserializeObject<uint>(@event["TargetIndex"]);
    }

    public static string Message(this Event ev)
    {
        return ev["Message"];
    }
}
file static class Extensions
{
    public static void TTS(this ScriptAccessory accessory, string text)
    {
        accessory.Method.TTS(text);
    }
}

file static class IbcHelper
{
    public static IGameObject? GetById(this ScriptAccessory sa, ulong id)
    {
        return sa.Data.Objects.SearchById(id);
    }

    public static IGameObject? GetMe(this ScriptAccessory sa)
    {
        return sa.Data.Objects.LocalPlayer;
    }

    public static IEnumerable<IGameObject?> GetByDataId(this ScriptAccessory sa, uint dataId)
    {
        return sa.Data.Objects.Where(x => x.DataId == dataId);
    }

    public static string GetCharJob(this ScriptAccessory sa, uint id, bool fullName = false)
    {
        IPlayerCharacter? chara = (IPlayerCharacter?)sa.GetById(id);
        if (chara == null) return "None";
        return fullName ? chara.ClassJob.Value.Name.ToString() : chara.ClassJob.Value.Abbreviation.ToString();
    }
    public static CombatRole GetRole(this ICharacter c)
    {
        ClassJob? valueNullable = c.ClassJob.ValueNullable;
        ref ClassJob? local1 = ref valueNullable;
        ClassJob valueOrDefault;
        byte? nullable1;
        if (!local1.HasValue)
        {
            nullable1 = new byte?();
        }
        else
        {
            valueOrDefault = local1.GetValueOrDefault();
            nullable1 = new byte?(valueOrDefault.Role);
        }
        byte? nullable2 = nullable1;
        if ((nullable2.HasValue ? new int?((int)nullable2.GetValueOrDefault()) : new int?()).GetValueOrDefault() == 1)
            return CombatRole.Tank;
        valueNullable = c.ClassJob.ValueNullable;
        ref ClassJob? local2 = ref valueNullable;
        byte? nullable3;
        if (!local2.HasValue)
        {
            nullable3 = new byte?();
        }
        else
        {
            valueOrDefault = local2.GetValueOrDefault();
            nullable3 = new byte?(valueOrDefault.Role);
        }
        byte? nullable4 = nullable3;
        if ((nullable4.HasValue ? new int?((int)nullable4.GetValueOrDefault()) : new int?()).GetValueOrDefault() == 2)
            return CombatRole.DPS;
        valueNullable = c.ClassJob.ValueNullable;
        ref ClassJob? local3 = ref valueNullable;
        byte? nullable5;
        if (!local3.HasValue)
        {
            nullable5 = new byte?();
        }
        else
        {
            valueOrDefault = local3.GetValueOrDefault();
            nullable5 = new byte?(valueOrDefault.Role);
        }
        byte? nullable6 = nullable5;
        if ((nullable6.HasValue ? new int?((int)nullable6.GetValueOrDefault()) : new int?()).GetValueOrDefault() == 3)
            return CombatRole.DPS;
        valueNullable = c.ClassJob.ValueNullable;
        ref ClassJob? local4 = ref valueNullable;
        byte? nullable7;
        if (!local4.HasValue)
        {
            nullable7 = new byte?();
        }
        else
        {
            valueOrDefault = local4.GetValueOrDefault();
            nullable7 = new byte?(valueOrDefault.Role);
        }
        byte? nullable8 = nullable7;
        return (nullable8.HasValue ? new int?((int)nullable8.GetValueOrDefault()) : new int?()).GetValueOrDefault() == 4 ? CombatRole.Healer : CombatRole.NonCombat;
    }

    public static bool IsTank(this ScriptAccessory sa, uint id)
    {
        IPlayerCharacter? chara = (IPlayerCharacter?)sa.GetById(id);
        if (chara == null) return false;
        return chara.GetRole() == CombatRole.Tank;
    }
    public static bool IsHealer(this ScriptAccessory sa, uint id)
    {
        IPlayerCharacter? chara = (IPlayerCharacter?)sa.GetById(id);
        if (chara == null) return false;
        return chara.GetRole() == CombatRole.Healer;
    }
    public static bool IsDps(this ScriptAccessory sa, uint id)
    {
        IPlayerCharacter? chara = (IPlayerCharacter?)sa.GetById(id);
        if (chara == null) return false;
        return chara.GetRole() == CombatRole.DPS;
    }
    public static bool AtNorth(this ScriptAccessory sa, uint id, float centerZ)
    {
        var chara = sa.GetById(id);
        if (chara == null) return false;
        return chara.Position.Z <= centerZ;
    }
    public static bool AtSouth(this ScriptAccessory sa, uint id, float centerZ)
    {
        var chara = sa.GetById(id);
        if (chara == null) return false;
        return chara.Position.Z > centerZ;
    }
    public static bool AtWest(this ScriptAccessory sa, uint id, float centerX)
    {
        var chara = sa.GetById(id);
        if (chara == null) return false;
        return chara.Position.X <= centerX;
    }
    public static bool AtEast(this ScriptAccessory sa, uint id, float centerX)
    {
        var chara = sa.GetById(id);
        if (chara == null) return false;
        return chara.Position.X > centerX;
    }

    public static bool HasStatus(this ScriptAccessory sa, uint id, uint statusId)
    {
        IGameObject? chara = sa.GetById(id);
        if (chara == null || !chara.IsValid()) return false;
        unsafe
        {
            BattleChara* charaStruct = (BattleChara*)chara.Address;
            return charaStruct->GetStatusManager()->HasStatus(statusId, id);
        }
    }
}

file static class DirectionCalc
{
    // 以北为0建立list
    // Game         List    Logic
    // 0            - 4     pi
    // 0.25 pi      - 3     0.75pi
    // 0.5 pi       - 2     0.5pi
    // 0.75 pi      - 1     0.25pi
    // pi           - 0     0
    // 1.25 pi      - 7     1.75pi
    // 1.5 pi       - 6     1.5pi
    // 1.75 pi      - 5     1.25pi
    // Logic = Pi - Game (+ 2pi)

    /// <summary>
    /// 将游戏基角度（以南为0，逆时针增加）转为逻辑基角度（以北为0，顺时针增加）
    /// 算法与Logic2Game完全相同，但为了代码可读性，便于区分。
    /// </summary>
    /// <param name="radian">游戏基角度</param>
    /// <returns>逻辑基角度</returns>
    public static float Game2Logic(this float radian)
    {
        // if (r < 0) r = (float)(r + 2 * Math.PI);
        // if (r > 2 * Math.PI) r = (float)(r - 2 * Math.PI);

        var r = float.Pi - radian;
        r = (r + float.Pi * 2) % (float.Pi * 2);
        return r;
    }

    /// <summary>
    /// 将逻辑基角度（以北为0，顺时针增加）转为游戏基角度（以南为0，逆时针增加）
    /// 算法与Game2Logic完全相同，但为了代码可读性，便于区分。
    /// </summary>
    /// <param name="radian">逻辑基角度</param>
    /// <returns>游戏基角度</returns>
    public static float Logic2Game(this float radian)
    {
        // var r = (float)Math.PI - radian;
        // if (r < Math.PI) r = (float)(r + 2 * Math.PI);
        // if (r > Math.PI) r = (float)(r - 2 * Math.PI);

        return radian.Game2Logic();
    }

    /// <summary>
    /// 适用于旋转，FF14游戏基顺时针旋转为负。
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static float Cw2Ccw(this float radian)
    {
        return -radian;
    }

    /// <summary>
    /// 适用于旋转，FF14游戏基顺时针旋转为负。
    /// 与Cw2CCw完全相同，为了代码可读性便于区分。
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static float Ccw2Cw(this float radian)
    {
        return -radian;
    }

    /// <summary>
    /// 输入逻辑基角度，获取逻辑方位（斜分割以正上为0，正分割以右上为0，顺时针增加）
    /// </summary>
    /// <param name="radian">逻辑基角度</param>
    /// <param name="dirs">方位总数</param>
    /// <param name="diagDivision">斜分割，默认true</param>
    /// <returns>逻辑基角度对应的逻辑方位</returns>
    public static int Rad2Dirs(this float radian, int dirs, bool diagDivision = true)
    {
        var r = diagDivision
            ? Math.Round(radian / (2f * float.Pi / dirs))
            : Math.Floor(radian / (2f * float.Pi / dirs));
        r = (r + dirs) % dirs;
        return (int)r;
    }

    /// <summary>
    /// 输入坐标，获取逻辑方位（斜分割以正上为0，正分割以右上为0，顺时针增加）
    /// </summary>
    /// <param name="point">坐标点</param>
    /// <param name="center">中心点</param>
    /// <param name="dirs">方位总数</param>
    /// <param name="diagDivision">斜分割，默认true</param>
    /// <returns>该坐标点对应的逻辑方位</returns>
    public static int Position2Dirs(this Vector3 point, Vector3 center, int dirs, bool diagDivision = true)
    {
        double dirsDouble = dirs;
        var r = diagDivision
            ? Math.Round(dirsDouble / 2 - dirsDouble / 2 * Math.Atan2(point.X - center.X, point.Z - center.Z) / Math.PI) % dirsDouble
            : Math.Floor(dirsDouble / 2 - dirsDouble / 2 * Math.Atan2(point.X - center.X, point.Z - center.Z) / Math.PI) % dirsDouble;
        return (int)r;
    }

    /// <summary>
    /// 以逻辑基弧度旋转某点
    /// </summary>
    /// <param name="point">待旋转点坐标</param>
    /// <param name="center">中心</param>
    /// <param name="radian">旋转弧度</param>
    /// <returns>旋转后坐标点</returns>
    public static Vector3 RotatePoint(this Vector3 point, Vector3 center, float radian)
    {
        // 围绕某点顺时针旋转某弧度
        Vector2 v2 = new(point.X - center.X, point.Z - center.Z);
        var rot = MathF.PI - MathF.Atan2(v2.X, v2.Y) + radian;
        var length = v2.Length();
        return new Vector3(center.X + MathF.Sin(rot) * length, center.Y, center.Z - MathF.Cos(rot) * length);
    }

    /// <summary>
    /// 以逻辑基角度从某中心点向外延伸
    /// </summary>
    /// <param name="center">待延伸中心点</param>
    /// <param name="radian">旋转弧度</param>
    /// <param name="length">延伸长度</param>
    /// <returns>延伸后坐标点</returns>
    public static Vector3 ExtendPoint(this Vector3 center, float radian, float length)
    {
        // 令某点以某弧度延伸一定长度
        return new Vector3(center.X + MathF.Sin(radian) * length, center.Y, center.Z - MathF.Cos(radian) * length);
    }
    /// <summary>
    /// 在原始点与中心点的连线上，计算距离原始点指定距离的新坐标点
    /// </summary>
    /// <param name="originalPoint">原始坐标点</param>
    /// <param name="center">中心点</param>
    /// <param name="distance">目标点与原始点的距离（可正可负，符号决定方向）</param>
    /// <returns>计算得到的新坐标点</returns>
    /// <exception cref="ArgumentException">当原始点与中心点重合时抛出</exception>
    public static Vector3 CalculatePointOnLine(this Vector3 originalPoint, Vector3 center, float distance)
    {
        var originalPoint2D = new Vector2(originalPoint.X, originalPoint.Z);
        var center2D = new Vector2(center.X, center.Z);
        // 处理原始点与中心点重合的情况
        if (originalPoint2D == center2D)
        {
            throw new ArgumentException("原始点与中心点不能重合", nameof(originalPoint));
        }
        
        // 计算从中心点到原始点的方向向量并归一化
        Vector2 direction = Vector2.Normalize(originalPoint2D - center2D);
        var result = originalPoint2D + direction * distance;
        // 计算新点：原始点沿着方向向量移动指定距离
        return new Vector3(result.X, 0, result.Y);
    }

    /// <summary>
    /// 寻找外侧某点到中心的逻辑基弧度
    /// </summary>
    /// <param name="center">中心</param>
    /// <param name="newPoint">外侧点</param>
    /// <returns>外侧点到中心的逻辑基弧度</returns>
    public static float FindRadian(this Vector3 newPoint, Vector3 center)
    {
        var radian = MathF.PI - MathF.Atan2(newPoint.X - center.X, newPoint.Z - center.Z);
        if (radian < 0)
            radian += 2 * MathF.PI;
        return radian;
    }

    /// <summary>
    /// 将输入点左右折叠
    /// </summary>
    /// <param name="point">待折叠点</param>
    /// <param name="centerX">中心折线坐标点</param>
    /// <returns></returns>
    public static Vector3 FoldPointHorizon(this Vector3 point, float centerX)
    {
        return point with { X = 2 * centerX - point.X };
    }

    /// <summary>
    /// 将输入点上下折叠
    /// </summary>
    /// <param name="point">待折叠点</param>
    /// <param name="centerZ">中心折线坐标点</param>
    /// <returns></returns>
    public static Vector3 FoldPointVertical(this Vector3 point, float centerZ)
    {
        return point with { Z = 2 * centerZ - point.Z };
    }

    /// <summary>
    /// 将输入点中心对称
    /// </summary>
    /// <param name="point">输入点</param>
    /// <param name="center">中心点</param>
    /// <returns></returns>
    public static Vector3 PointCenterSymmetry(this Vector3 point, Vector3 center)
    {
        return point.RotatePoint(center, float.Pi);
    }

    /// <summary>
    /// 将输入点朝某中心点往内/外同角度延伸，默认向内
    /// </summary>
    /// <param name="point">待延伸点</param>
    /// <param name="center">中心点</param>
    /// <param name="length">延伸长度</param>
    /// <param name="isOutside">是否向外延伸</param>>
    /// <returns></returns>
    public static Vector3 PointInOutside(this Vector3 point, Vector3 center, float length, bool isOutside = false)
    {
        Vector2 v2 = new(point.X - center.X, point.Z - center.Z);
        var targetPos = (point - center) / v2.Length() * length * (isOutside ? 1 : -1) + point;
        return targetPos;
    }

    /// <summary>
    /// 获得两点之间距离
    /// </summary>
    /// <param name="point"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float DistanceTo(this Vector3 point, Vector3 target)
    {
        Vector2 v2 = new(point.X - target.X, point.Z - target.Z);
        return v2.Length();
    }

    /// <summary>
    /// 寻找两点之间的角度差，范围0~360deg
    /// </summary>
    /// <param name="basePoint">基准位置</param>
    /// <param name="targetPos">比较目标位置</param>
    /// <param name="center">场地中心</param>
    /// <returns></returns>
    public static float FindRadianDifference(this Vector3 targetPos, Vector3 basePoint, Vector3 center)
    {
        var baseRad = basePoint.FindRadian(center);
        var targetRad = targetPos.FindRadian(center);
        var deltaRad = targetRad - baseRad;
        if (deltaRad < 0)
            deltaRad += float.Pi * 2;
        return deltaRad;
    }

    /// <summary>
    /// 从第三人称视角出发观察某目标是否在另一目标的右侧。
    /// </summary>
    /// <param name="basePoint">基准位置</param>
    /// <param name="targetPos">比较目标位置</param>
    /// <param name="center">场地中心</param>
    /// <returns></returns>
    public static bool IsAtRight(this Vector3 targetPos, Vector3 basePoint, Vector3 center)
    {
        // 从场中看向场外，在右侧
        return targetPos.FindRadianDifference(basePoint, center) < float.Pi;
    }

    /// <summary>
    /// 获取给定数的指定位数
    /// </summary>
    /// <param name="val">给定数值</param>
    /// <param name="x">对应位数，个位为1</param>
    /// <returns></returns>
    public static int GetDecimalDigit(this int val, int x)
    {
        string valStr = val.ToString();
        int length = valStr.Length;

        if (x < 1 || x > length)
        {
            return -1;
        }

        char digitChar = valStr[length - x]; // 从右往左取第x位
        return int.Parse(digitChar.ToString());
    }
}

file static class IndexHelper
{
    /// <summary>
    /// 输入玩家dataId，获得对应的位置index
    /// </summary>
    /// <param name="pid">玩家SourceId</param>
    /// <param name="accessory"></param>
    /// <returns>该玩家对应的位置index</returns>
    public static int GetPlayerIdIndex(this ScriptAccessory accessory, ulong pid)
    {
        // 获得玩家 IDX
        return accessory.Data.PartyList.IndexOf((uint)pid);
    }

    /// <summary>
    /// 获得主视角玩家对应的位置index
    /// </summary>
    /// <param name="accessory"></param>
    /// <returns>主视角玩家对应的位置index</returns>
    public static int GetMyIndex(this ScriptAccessory accessory)
    {
        return accessory.Data.PartyList.IndexOf(accessory.Data.Me);
    }

    /// <summary>
    /// 输入玩家dataId，获得对应的位置称呼，输出字符仅作文字输出用
    /// </summary>
    /// <param name="pid">玩家SourceId</param>
    /// <param name="accessory"></param>
    /// <returns>该玩家对应的位置称呼</returns>
    public static string GetPlayerJobById(this ScriptAccessory accessory, uint pid)
    {
        // 获得玩家职能简称，无用处，仅作DEBUG输出
        var idx = accessory.Data.PartyList.IndexOf(pid);
        var str = accessory.GetPlayerJobByIndex(idx);
        return str;
    }

    /// <summary>
    /// 输入位置index，获得对应的位置称呼，输出字符仅作文字输出用
    /// </summary>
    /// <param name="idx">位置index</param>
    /// <param name="fourPeople">是否为四人迷宫</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static string GetPlayerJobByIndex(this ScriptAccessory accessory, int idx, bool fourPeople = false)
    {
        var str = idx switch
        {
            0 => "MT",
            1 => fourPeople ? "H1" : "ST",
            2 => fourPeople ? "D1" : "H1",
            3 => fourPeople ? "D2" : "H2",
            4 => "D1",
            5 => "D2",
            6 => "D3",
            7 => "D4",
            _ => "unknown"
        };
        return str;
    }
}


#region 绘图函数

file static class AssignDp
{
    /// <summary>
    /// 返回箭头指引相关dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerObj">箭头起始，可输入uint或Vector3</param>
    /// <param name="targetObj">箭头指向目标，可输入uint或Vector3，为0则无目标</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="rotation">箭头旋转角度</param>
    /// <param name="scale">箭头宽度</param>
    /// <param name="isSafe">使用安全色</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawGuidance(this ScriptAccessory accessory,
        object ownerObj, object targetObj, int delay, int destroy, string name, float rotation = 0, float scale = 1f, bool isSafe = true)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Rotation = rotation;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Color = isSafe ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;

        switch (ownerObj)
        {
            case uint sid:
                dp.Owner = sid;
                break;
            case ulong sid:
                dp.Owner = sid;
                break;
            case Vector3 spos:
                dp.Position = spos;
                break;
            default:
                throw new ArgumentException("ownerObj的目标类型输入错误");
        }

        switch (targetObj)
        {
            case uint tid:
                if (tid != 0) dp.TargetObject = tid;
                break;
            case ulong tid:
                if (tid != 0) dp.TargetObject = tid;
                break;
            case Vector3 tpos:
                dp.TargetPosition = tpos;
                break;
        }

        return dp;
    }

    public static DrawPropertiesEdit DrawGuidance(this ScriptAccessory accessory,
        object targetObj, int delay, int destroy, string name, float rotation = 0, float scale = 1f, bool isSafe = true)
    => accessory.DrawGuidance(accessory.Data.Me, targetObj, delay, destroy, name, rotation, scale, isSafe);

    // {
    //     return targetObj switch
    //     {
    //         uint uintTarget => accessory.DrawGuidance(accessory.Data.Me, uintTarget, delay, destroy, name, rotation, scale),
    //         Vector3 vectorTarget => accessory.DrawGuidance(accessory.Data.Me, vectorTarget, delay, destroy, name, rotation, scale),
    //         _ => throw new ArgumentException("targetObj 的类型必须是 uint 或 Vector3")
    //     };
    // }

    /// <summary>
    /// 返回扇形左右刀
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="isLeftCleave">是左刀</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawLeftRightCleave(this ScriptAccessory accessory, uint ownerId, bool isLeftCleave, int delay, int destroy, string name, float radian = float.Pi, float scale = 60f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Radian = radian;
        dp.Rotation = isLeftCleave ? float.Pi / 2 : -float.Pi / 2;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回扇形前后刀
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="isFrontCleave">是前刀</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFrontBackCleave(this ScriptAccessory accessory, uint ownerId, bool isFrontCleave, int delay, int destroy, string name, float radian = float.Pi, float scale = 60f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Radian = radian;
        dp.Rotation = isFrontCleave ? 0 : -float.Pi;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回距离某对象目标最近/最远的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="orderIdx">顺序，从1开始</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="isNear">true为最近，false为最远</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawTargetNearFarOrder(this ScriptAccessory accessory, ulong ownerId, uint orderIdx,
        bool isNear, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.CentreResolvePattern =
            isNear ? PositionResolvePatternEnum.PlayerNearestOrder : PositionResolvePatternEnum.PlayerFarestOrder;
        dp.CentreOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回距离某坐标位置最近/最远的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="position">特定坐标点</param>
    /// <param name="orderIdx">顺序，从1开始</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="isNear">true为最近，false为最远</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawPositionNearFarOrder(this ScriptAccessory accessory, Vector3 position, uint orderIdx,
        bool isNear, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Position = position;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.TargetResolvePattern =
            isNear ? PositionResolvePatternEnum.PlayerNearestOrder : PositionResolvePatternEnum.PlayerFarestOrder;
        dp.TargetOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回ownerId施法目标的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawOwnersTarget(this ScriptAccessory accessory, uint ownerId, float width, float length, int delay,
        int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.TargetResolvePattern = PositionResolvePatternEnum.OwnerTarget;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回ownerId仇恨相关的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="orderIdx">仇恨顺序，从1开始</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawOwnersEnmityOrder(this ScriptAccessory accessory, uint ownerId, uint orderIdx, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.CentreResolvePattern = PositionResolvePatternEnum.OwnerEnmityOrder;
        dp.CentreOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回owner与target的dp，可修改 dp.Owner, dp.TargetObject, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="rotation">绘图旋转角度</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawTarget2Target(this ScriptAccessory accessory, uint ownerId, uint targetId, float width, float length, int delay, int destroy, string name, float rotation = 0, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Rotation = rotation;
        dp.Owner = ownerId;
        dp.TargetObject = targetId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回画向某目标的扇形绘图
    /// </summary>
    /// <param name="sourceId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="color">绘图颜色</param>
    /// <param name="rotation">旋转角度</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFanToTarget(this ScriptAccessory accessory, uint sourceId, uint targetId, float radian, float scale, int delay, int destroy, string name, Vector4 color, float rotation = 0, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.DrawTarget2Target(sourceId, targetId, scale, scale, delay, destroy, name, rotation, lengthByDistance, byTime);
        dp.Radian = radian;
        dp.Color = color;
        return dp;
    }

    /// <summary>
    /// 返回owner与target之间的连线dp，使用Line绘制
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="scale">线条宽度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawConnectionBetweenTargets(this ScriptAccessory accessory, uint ownerId,
        uint targetId, int delay, int destroy, string name, float scale = 1f)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Owner = ownerId;
        dp.TargetObject = targetId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= ScaleMode.YByDistance;
        return dp;
    }

    /// <summary>
    /// 返回圆形dp，跟随owner，可修改 dp.Owner, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="scale">圆圈尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawCircle(this ScriptAccessory accessory, ulong ownerId, float scale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回环形dp，跟随owner，可修改 dp.Owner, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="scale">外环实心尺寸</param>
    /// <param name="innerScale">内环空心尺寸</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawDonut(this ScriptAccessory accessory, uint ownerId, float scale, float innerScale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.DrawFan(ownerId, float.Pi * 2, 0, scale, innerScale, delay, destroy, name, byTime);
        return dp;
    }

    /// <summary>
    /// 返回静态dp，通常用于指引固定位置。可修改 dp.Position, dp.Rotation, dp.Scale
    /// </summary>
    /// <param name="ownerObj">绘图起始，可输入uint或Vector3</param>
    /// <param name="targetObj">绘图目标，可输入uint或Vector3，为0则无目标</param>
    /// <param name="radian">图形角度</param>
    /// <param name="rotation">旋转角度，以北为0度顺时针</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="color">是Vector4则选用该颜色</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStatic(this ScriptAccessory accessory, object ownerObj, object targetObj,
        float radian, float rotation, float width, float length, object color, int delay, int destroy, string name)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        switch (ownerObj)
        {
            case uint sid:
                dp.Owner = sid;
                break;
            case ulong sid:
                dp.Owner = sid;
                break;
            case Vector3 spos:
                dp.Position = spos;
                break;
        }
        switch (targetObj)
        {
            case uint tid:
                if (tid != 0) dp.TargetObject = tid;
                break;
            case ulong tid:
                if (tid != 0) dp.TargetObject = tid;
                break;
            case Vector3 tpos:
                dp.TargetPosition = tpos;
                break;
        }
        dp.Radian = radian;
        dp.Rotation = rotation.Logic2Game();
        switch (color)
        {
            case Vector4 clr:
                dp.Color = clr;
                break;
            default:
                dp.Color = accessory.Data.DefaultDangerColor;
                break;
        }
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        return dp;
    }

    /// <summary>
    /// 返回静态圆圈dp，通常用于指引固定位置。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="center">圆圈中心位置</param>
    /// <param name="color">圆圈颜色</param>
    /// <param name="scale">圆圈尺寸，默认1.5f</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStaticCircle(this ScriptAccessory accessory, Vector3 center, Vector4 color,
        int delay, int destroy, string name, float scale = 1.5f)
        => accessory.DrawStatic(center, (uint)0, 0, 0, scale, scale, color, delay, destroy, name);
    // {
    //     var dp = accessory.DrawStatic(center, (uint)0, 0, 0, scale, scale, color, delay, destroy, name);
    //     return dp;
    // }

    /// <summary>
    /// 返回静态月环dp，通常用于指引固定位置。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="center">月环中心位置</param>
    /// <param name="color">月环颜色</param>
    /// <param name="scale">月环外径，默认1.5f</param>
    /// <param name="innerscale">月环内径，默认scale-0.05f</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStaticDonut(this ScriptAccessory accessory, Vector3 center, Vector4 color,
        int delay, int destroy, string name, float scale, float innerscale = 0)
        => accessory.DrawStatic(center, (uint)0,
        float.Pi * 2, 0, scale, scale, color, delay, destroy, name);

    // {
    //     var dp = accessory.DrawStatic(center, (uint)0, float.Pi * 2, 0, scale, scale, color, delay, destroy, name);
    //     dp.InnerScale = innerscale != 0f ? new Vector2(innerscale) : new Vector2(scale - 0.05f);
    //     return dp;
    // }

    /// <summary>
    /// 返回矩形
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="width">矩形宽度</param>
    /// <param name="length">矩形长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawRect(this ScriptAccessory accessory, uint ownerId, float width, float length, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回扇形
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="radian">扇形弧度</param>
    /// <param name="rotation">图形旋转角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="innerScale">扇形内环空心尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFan(this ScriptAccessory accessory, uint ownerId, float radian, float rotation, float scale, float innerScale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.InnerScale = new Vector2(innerScale);
        dp.Radian = radian;
        dp.Rotation = rotation;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回击退
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="target">击退源，可输入uint或Vector3</param>
    /// <param name="width">击退绘图宽度</param>
    /// <param name="length">击退绘图长度/距离</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="ownerId">起始目标ID，通常为自己或其他玩家</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawKnockBack(this ScriptAccessory accessory, uint ownerId, object target, float length, int delay, int destroy, string name, float width = 1.5f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        switch (target)
        {
            // 根据传入的 tid 类型来决定是使用 TargetObject 还是 TargetPosition
            case uint tid:
                dp.TargetObject = tid; // 如果 tid 是 uint 类型
                break;
            case ulong tid:
                dp.TargetObject = tid; // 如果 tid 是 ulong 类型
                break;
            case Vector3 tpos:
                dp.TargetPosition = tpos; // 如果 tid 是 Vector3 类型
                break;
            default:
                throw new ArgumentException("DrawKnockBack的目标类型输入错误");
        }
        dp.Rotation = float.Pi;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    public static DrawPropertiesEdit DrawKnockBack(this ScriptAccessory accessory, object target, float length,
        int delay, int destroy, string name, float width = 1.5f, bool byTime = false)
        => accessory.DrawKnockBack(accessory.Data.Me, target, length, delay, destroy, name, width, byTime);
    // {
    //     return target switch
    //     {
    //         uint uintTarget => accessory.DrawKnockBack(accessory.Data.Me, uintTarget, length, delay, destroy, name, width, byTime),
    //         Vector3 vectorTarget => accessory.DrawKnockBack(accessory.Data.Me, vectorTarget, length, delay, destroy, name, width, byTime),
    //         _ => throw new ArgumentException("target 的类型必须是 uint 或 Vector3")
    //     };
    // }

    /// <summary>
    /// 返回背对
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="target">背对源，可输入uint或Vector3</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="ownerId">起始目标ID，通常为自己或其他玩家</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawSightAvoid(this ScriptAccessory accessory, uint ownerId, object target, int delay, int destroy, string name)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Owner = ownerId;
        switch (target)
        {
            // 根据传入的 tid 类型来决定是使用 TargetObject 还是 TargetPosition
            case uint tid:
                dp.TargetObject = tid; // 如果 tid 是 uint 类型
                break;
            case ulong tid:
                dp.TargetObject = tid; // 如果 tid 是 ulong 类型
                break;
            case Vector3 tpos:
                dp.TargetPosition = tpos; // 如果 tid 是 Vector3 类型
                break;
            default:
                throw new ArgumentException("DrawSightAvoid的目标类型输入错误");
        }
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        return dp;
    }

    public static DrawPropertiesEdit DrawSightAvoid(this ScriptAccessory accessory, object target, int delay,
        int destroy, string name)
        => accessory.DrawSightAvoid(accessory.Data.Me, target, delay, destroy, name);
    // {
    //     return target switch
    //     {
    //         uint uintTarget => accessory.DrawSightAvoid(accessory.Data.Me, uintTarget, delay, destroy, name),
    //         Vector3 vectorTarget => accessory.DrawSightAvoid(accessory.Data.Me, vectorTarget, delay, destroy, name),
    //         _ => throw new ArgumentException("target 的类型必须是 uint 或 Vector3")
    //     };
    // }

    /// <summary>
    /// 返回多方向延伸指引
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="owner">分散源</param>
    /// <param name="extendDirs">分散角度</param>
    /// <param name="myDirIdx">玩家对应角度idx</param>
    /// <param name="width">指引箭头宽度</param>
    /// <param name="length">指引箭头长度</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="colorPlayer">玩家对应箭头指引颜色</param>
    /// <param name="colorNormal">其他玩家对应箭头指引颜色</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<DrawPropertiesEdit> DrawExtendDirection(this ScriptAccessory accessory, object owner,
        List<float> extendDirs, int myDirIdx, float width, float length, int delay, int destroy, string name,
        Vector4 colorPlayer, Vector4 colorNormal)
    {
        List<DrawPropertiesEdit> dpList = [];
        switch (owner)
        {
            case uint sid:
                for (var i = 0; i < extendDirs.Count; i++)
                {
                    var dp = accessory.DrawGuidance(owner, sid, delay, destroy, $"{name}{i}", extendDirs[i], width);
                    dp.Color = i == myDirIdx ? colorPlayer : colorNormal;
                    dpList.Add(dp);
                }
                break;
            case Vector3 spos:
                for (var i = 0; i < extendDirs.Count; i++)
                {
                    var dp = accessory.DrawGuidance(spos, spos.ExtendPoint(extendDirs[i], length), delay, destroy,
                        $"{name}{i}", 0, width);
                    dp.Color = i == myDirIdx ? colorPlayer : colorNormal;
                    dpList.Add(dp);
                }
                break;
            default:
                throw new ArgumentException("DrawExtendDirection的目标类型输入错误");
        }

        return dpList;
    }

    /// <summary>
    /// 返回多地点指路指引列表
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="positions">地点位置</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="colorPosPlayer">对应位置标记行动颜色</param>
    /// <param name="colorPosNormal">对应位置标记准备颜色</param>
    /// <param name="colorGo">指路出发箭头颜色</param>
    /// <param name="colorPrepare">指路准备箭头颜色</param>
    /// <returns>dpList中的三个List：位置标记，玩家指路箭头，地点至下个地点的指路箭头</returns>
    public static List<List<DrawPropertiesEdit>> DrawMultiGuidance(this ScriptAccessory accessory,
        List<Vector3> positions, List<int> delay, List<int> destroy, string name,
        Vector4 colorGo, Vector4 colorPrepare, Vector4 colorPosNormal, Vector4 colorPosPlayer)
    {
        List<List<DrawPropertiesEdit>> dpList = [[], [], []];
        for (var i = 0; i < positions.Count; i++)
        {
            var dpPos = accessory.DrawStaticCircle(positions[i], colorPosPlayer, delay[i], destroy[i], $"{name}pos{i}");
            dpList[0].Add(dpPos);
            var dpGuide = accessory.DrawGuidance(positions[i], colorGo, delay[i], destroy[i], $"{name}guide{i}");
            dpList[1].Add(dpGuide);
            if (i == positions.Count - 1) break;
            var dpPrep = accessory.DrawGuidance(positions[i], positions[i + 1], delay[i], destroy[i], $"{name}prep{i}");
            dpList[2].Add(dpPrep);
        }
        return dpList;
    }

    public static void DebugMsg(this ScriptAccessory accessory, string str, bool debugMode = false, bool debugChat = false)
    {
        if (!debugMode)
            return;
        accessory.Log.Debug($"/e [DEBUG] {str}");

        if (!debugChat)
            return;
        accessory.Method.SendChat($"/e [DEBUG] {str}");
    }

    /// <summary>
    /// 将List内信息转换为字符串。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="myList"></param>
    /// <param name="isJob">是职业，在转为字符串前调用转职业函数</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string BuildListStr<T>(this ScriptAccessory accessory, List<T> myList, bool isJob = false)
    {
        return string.Join(", ", myList.Select(item =>
        {
            if (isJob && item != null && item is int i)
                return accessory.GetPlayerJobByIndex(i);
            return item?.ToString() ?? "";
        }));
    }
}

#endregion 绘图函数

#region 特殊函数

file static class SpecialFunction
{
    public static void SetTargetable(this ScriptAccessory sa, IGameObject? obj, bool targetable)
    {
        if (obj == null || !obj.IsValid())
        {
            sa.Log.Error($"传入的IGameObject不合法。");
            return;
        }
        unsafe
        {
            GameObject* charaStruct = (GameObject*)obj.Address;
            if (targetable)
            {
                if (obj.IsDead || obj.IsTargetable) return;
                charaStruct->TargetableStatus |= ObjectTargetableFlags.IsTargetable;
            }
            else
            {
                if (!obj.IsTargetable) return;
                charaStruct->TargetableStatus &= ~ObjectTargetableFlags.IsTargetable;
            }
        }
        sa.Log.Debug($"SetTargetable {targetable} => {obj.Name} {obj}");
    }

    public static void SetRotation(this ScriptAccessory sa, IGameObject? obj, float rotation)
    {
        if (obj == null || !obj.IsValid())
        {
            sa.Log.Error($"传入的IGameObject不合法。");
            return;
        }
        unsafe
        {
            GameObject* charaStruct = (GameObject*)obj.Address;
            charaStruct->SetRotation(rotation);
        }
        sa.Log.Debug($"SetRotation => {obj.Name.TextValue} | {obj} => {rotation}");
    }

    public static void SetPostion(this ScriptAccessory sa, IGameObject? obj, Vector3 postion)
    {
        if (obj == null || !obj.IsValid())
        {
            sa.Log.Error($"传入的IGameObject不合法。");
            return;
        }
        unsafe
        {
            GameObject* charaStruct = (GameObject*)obj.Address;
            charaStruct->SetPosition(postion.X, postion.Y, postion.Z);
        }
        sa.Log.Debug($"SetRotation => {obj.Name.TextValue} | {obj} => {postion}");
    }
}

#endregion 特殊函数

#endregion 函数集

