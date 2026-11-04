using System.Xml.Linq;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrnamentPressTests
{
    [Fact]
    public void Every_pair_support_and_four_bit_law_preserve_the_difference_between_prediction_and_arrival()
    {
        for(int pair=0;pair<16;pair++)
        for(int law=0;law<16;law++)
        {
            var got=OrnamentPress.Fire(0,0,(byte)pair,(byte)law);
            var allowed=Enumerable.Range(0,4).Where(v=>(pair & 1<<v)!=0).ToArray();
            int Support(IEnumerable<int> values)=>values.Aggregate(0,(sum,v)=>sum|1<<v);
            var left=allowed.Select(v=>v&1).Distinct().ToArray();
            var right=allowed.Select(v=>v>>1).Distinct().ToArray();
            Assert.Equal(Support(allowed.Select(v=>(law>>v)&1)),(int)got.Prediction);
            int actual=Support(from a in left from b in right select (law>>(a+2*b))&1);
            Assert.Equal(actual,(int)got.Arrival);
            Assert.Equal(actual==2,got.Finial);
            Assert.Equal((left.SequenceEqual([1])?1:0)|(right.SequenceEqual([1])?2:0),(int)got.Arms);
            Assert.Equal(allowed.Length==1,got.Finished);
        }
        var held=OrnamentPress.Fire(0,0,6,14);
        Assert.Equal((2,3,0,false),((int)held.Prediction,(int)held.Arrival,(int)held.Arms,held.Finished));
        var partial=OrnamentPress.Fire(0,0,10,14);
        Assert.True(partial.Finial);
        Assert.False(partial.Finished);
    }

    [Fact]
    public void Both_flanks_are_directed_routes_from_the_same_fork_to_the_same_eye()
    {
        var all=OrnamentPlates.All.Append(OrnamentPlates.All[^1].Inlay!.Value.Child);
        foreach(var d in all)
        foreach(int tool in new[]{1,2})
        {
            var ends=new List<(int A,int B)>();
            foreach(int arity in new[]{2,4})
            for(int i=0;i<d.RowCount(tool,arity);i++)
            {
                var row=d.Stroke(tool,arity,i);
                Assert.All(row,id=>Assert.InRange(id,0,d.SiteCount-1));
                ends.Add((row[0],row[^1]));
            }
            var reached=new HashSet<int>{d.Fork};
            int before;
            do
            {
                before=reached.Count;
                foreach(var (a,b) in ends) if(reached.Contains(a)) reached.Add(b);
            } while(before!=reached.Count);
            Assert.Contains(d.Eye,reached);
        }
    }

    [Fact]
    public void Nested_pen_runs_only_on_a_real_parent_join_and_uses_its_own_six_sectors()
    {
        var arcade=OrnamentPlates.All[4];
        var ink=new OrnamentImpression(arcade,0,14);
        Assert.Equal(8,ink.Cells.Count(c=>c.Finial));
        Assert.Equal(8*(3+6*3),ink.Passes);
        Assert.Equal(Enumerable.Range(0,6),ink.Timeline.Skip(3).Take(18).Select(x=>x.ChildCell).Distinct());
        Assert.DoesNotContain(ink.Draw(3).Paths,p=>p.Nested);
        Assert.Contains(ink.Draw(4).Paths,p=>p.Nested);
        var strict=new OrnamentImpression(arcade,0,8);
        Assert.Equal(4,strict.Cells.Count(c=>c.Finial));
        Assert.Equal(8*3+4*18,strict.Passes);
        Assert.DoesNotContain(strict.Draw(strict.Passes).Paths,p=>p.Nested && p.Cell%2==1);
        var shut=new OrnamentImpression(arcade,5,8);
        Assert.All(shut.Cells,c=>Assert.True(c.Finished));
        Assert.DoesNotContain(shut.Timeline,p=>p.ChildCell>=0);
    }

    [Fact]
    public void All_prints_obey_their_repeat_and_clock_contract_and_export_the_observed_paths()
    {
        foreach(var d in OrnamentPlates.All.Append(OrnamentPlates.PairedKey))
        for(int rhythm=0;rhythm<11;rhythm++)
        foreach(byte law in new byte[]{14,8,6})
        {
            var p=new OrnamentImpression(d,rhythm,law);
            Assert.Equal(rhythm switch {8=>3,9 or 10=>2,_=>d.Count},p.Cells.Length);
            for(int i=0;i<p.Cells.Length;i++)
            {
                Assert.Equal(i%p.Tape.Length,p.Cells[i].Phase);
                Assert.Equal(p.Tape[p.Cells[i].Phase],p.Cells[i].Pair);
            }
            var paths=p.Draw(p.Passes).Paths;
            Assert.All(paths.Where(s=>!s.Nested),s=>
            {
                var c=p.Cells[s.Cell];
                Assert.True(s.Tool switch {0=>true,1=>(c.Arms&1)!=0,2=>(c.Arms&2)!=0,_=>c.Finial});
            });
            var svg=XDocument.Parse(p.Svg());
            Assert.Equal(paths.Select(x=>x.Path),svg.Descendants(XName.Get("path","http://www.w3.org/2000/svg"))
                .Select(x=>(string)x.Attribute("d")!));
            Assert.Equal(p.Draw(1).Paths.Single().Path,paths[0].Path);
        }
        var rosette=OrnamentPlates.All[0];
        Assert.True(rosette.Rim!.Read().RegularPolygon);
        var lace=OrnamentPlates.All[3];
        Assert.Equal(lace.PitchY/2,lace.Place(1,new(0,0)).Y);
        Assert.Equal(0,lace.Place(2,new(0,0)).Y);
        Assert.Equal(new byte[]{2,8},OrnamentPress.Strip(rosette,1));
        Assert.Equal(new byte[]{8,2},OrnamentPress.Strip(rosette,2));
        var square=OrnamentPlates.All[2].HookScore!;
        var grouped=OrnamentPlates.PairedKey.HookScore!;
        for(int voice=0;voice<2;voice++) Assert.Equal(square.VoiceBytes(voice),grouped.VoiceBytes(voice));
        Assert.Equal(8,square.Predict().Beats);
        Assert.Equal(4,grouped.Predict().Beats);
        for(int tool=1;tool<=2;tool++)
        {
            var a=OrnamentPlates.All[2]; var b=OrnamentPlates.PairedKey;
            Assert.Equal(4,a.RowCount(tool,2)-b.RowCount(tool,2));
            Assert.Equal(a.Point(a.Stroke(tool,2,a.RowCount(tool,2)-1)[^1]),
                b.Point(b.Stroke(tool,2,b.RowCount(tool,2)-1)[^1]));
        }
    }
}
