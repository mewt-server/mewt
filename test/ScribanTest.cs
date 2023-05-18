/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using Scriban;
using Scriban.Runtime;

namespace test;

public class ScribanTest
{
    public class CanUpdateReferencedObjectTestSubject
    {
        public required int Counter { get; set; }
        public required string Text { get; set; }
    }

    public class AsyncMethodsTestSubject : ScriptObject
    {
        public static async ValueTask<string> PrintAsync(string text)
            => await Task.Run<string>(() => text);
        public static string Print(string text)
            => text;
        public static string PrintType(object? obj)
            => obj?.GetType().ToString() ?? "null";
    }


    [Fact]
    public void CanUpdateReferencedObjectTest()
    {
        var subject = new CanUpdateReferencedObjectTestSubject()
        {
            Counter = 1,
            Text = "1"
        };
        var context = new TemplateContext();
        var script = new ScriptObject();
        script.Add("subject", subject);
        context.PushGlobal(script);
        var template = Template.Parse("{{ subject.counter += 1; subject.text = \"2\" }}");
        template.Render(context);
        Assert.Equivalent(2, subject.Counter);
        Assert.Equivalent("2", subject.Text);
    }

    public class DontCallUnecessaryPropertiesTestSubject
    {
        public required string Yes { get; init; }
        public string No => throw new NotImplementedException("This should not be called.");
    }

    [Fact]
    public void DontCallUnecessaryPropertiesTest()
    {
        var subject = new DontCallUnecessaryPropertiesTestSubject()
        {
            Yes = "Hello World"
        };
        var context = new TemplateContext();
        var script = new ScriptObject();
        script.Add("subject", subject);
        context.PushGlobal(script);
        var template = Template.Parse("{{ subject.yes }}");
        Assert.Equivalent(subject.Yes, template.Render(context));
    }

    [Fact]
    public async Task AsyncMethodsTest()
    {
        var context = new TemplateContext();
        var script = new ScriptObject();
        script.SetValue("subject", new AsyncMethodsTestSubject(), true);
        context.PushGlobal(script);
        var print = Template.Parse("{{ 'Hello World !' | subject.print | subject.print_type }}");
        Assert.Equivalent(typeof(string).ToString(), print.Render(context));
        Assert.Equivalent(typeof(string).ToString(), await print.RenderAsync(context));
        var print_async = Template.Parse("{{ 'Hello World !' | subject.print_async | subject.print_type }}");
        Assert.Equivalent(typeof(ValueTask<string>).ToString(), print_async.Render(context));
        Assert.Equivalent(typeof(string).ToString(), await print_async.RenderAsync(context));
    }

    [Fact]
    public async Task AsyncMethodsWithoutPrintTypeTest()
    {
        var context = new TemplateContext();
        var script = new ScriptObject();
        script.SetValue("subject", new AsyncMethodsTestSubject(), true);
        context.PushGlobal(script);
        var print = Template.Parse("{{ 'Hello World !' | subject.print }}");
        Assert.IsType<string>(print.Render(context));
        Assert.IsType<string>(await print.RenderAsync(context));
        var print_async = Template.Parse("{{ 'Hello World !' | subject.print_async }}");
        Assert.IsType<string>(print_async.Render(context));
        Assert.IsType<string>(await print_async.RenderAsync(context));
    }
}