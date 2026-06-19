namespace ScratchMill;

//using System.Collections.Generic;
//using NumbersSkia.Agent;
//using NumbersSkia.Demo;
//using NumbersSkia.Mappers;
//using NumbersSkia.Renderer;
//using static NumbersSkia.Renderer.CoreRenderer;

//public abstract class DemoBase : IDemos
//{
//    public List<PageCreator> Pages { get; } = new List<PageCreator>();
//    protected int Count => Pages.Count;
//    protected int _testIndex = 0;
//    protected MouseAgent _currentMouseAgent;
//    //protected List<int> _tests{get;} = new List<int>();

//    public SKWorkspaceMapper PreviousTest(MouseAgent mouseAgent)
//    {
//        int index = _testIndex >= 1 ? _testIndex - 1 : Pages.Count - 1;
//        return LoadTest(index, mouseAgent);
//    }
//    public SKWorkspaceMapper Reload(MouseAgent mouseAgent)
//    {
//        return LoadTest(_testIndex, mouseAgent);
//    }
//    public SKWorkspaceMapper NextTest(MouseAgent mouseAgent)
//    {
//        int index = _testIndex >= Pages.Count - 1 ? 0 : _testIndex + 1;
//        return LoadTest(index, mouseAgent);
//    }

//    public SKWorkspaceMapper LoadTest(int index, MouseAgent mouseAgent)
//    {
//        if (_currentMouseAgent != null)
//        {
//            _currentMouseAgent.Renderer.DrawingStart -= OnDrawingStart;
//            _currentMouseAgent.Renderer.DrawingComplete -= OnDrawingComplete;
//        }
//        _testIndex = index;
//        _currentMouseAgent = mouseAgent;
//        _currentMouseAgent.IsPaused = true;
//        _currentMouseAgent.ClearAll();
//        _currentMouseAgent.Renderer.DrawingStart += OnDrawingStart;
//        _currentMouseAgent.Renderer.DrawingComplete += OnDrawingComplete;

//        SKWorkspaceMapper wm = Pages[_testIndex]();
//        wm.EnsureRenderers();
//        _currentMouseAgent.IsPaused = false;
//        return wm;
//    }
//    protected virtual void OnDrawingStart(object? sender, CanvasEventArgs e)
//    {
//    }
//    protected virtual void OnDrawingComplete(object? sender, CanvasEventArgs e)
//    {
//    }
//}
