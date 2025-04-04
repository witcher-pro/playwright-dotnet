/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text;

namespace Microsoft.Playwright.Tests;

public class PageSetInputFilesTests : PageTestEx
{
    public static string FileToUpload = TestUtils.GetAsset("file-to-upload.txt");

    [PlaywrightTest("page-set-input-files.spec.ts", "should upload the file")]
    public async Task ShouldUploadTheFile()
    {
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", FileToUpload);
        var input = await Page.QuerySelectorAsync("input");
        await input.SetInputFilesAsync(filePath);
        Assert.AreEqual("file-to-upload.txt", await Page.EvaluateAsync<string>("e => e.files[0].name", input));
        Assert.AreEqual("contents of the file", await Page.EvaluateAsync<string>(@"e => {
                var reader = new FileReader();
                var promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(e.files[0]);
                return promise.then(() => reader.result);
            }", input));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<input type=file>");
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", FileToUpload);
        await Page.SetInputFilesAsync("input", filePath);

        Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
        Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should set from memory")]
    public async Task ShouldSetFromMemory()
    {
        await Page.SetContentAsync("<input type=file>");

        await Page.SetInputFilesAsync("input", new FilePayload
        {
            Name = "test.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes("this is a test"),
        });

        Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
        Assert.AreEqual("test.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should emit event once")]
    public async Task ShouldEmitEventOnce()
    {
        await Page.SetContentAsync("<input type=file>");
        var chooserTsc = new TaskCompletionSource<IElementHandle>();
        void EventHandler(object sender, IFileChooser e)
        {
            chooserTsc.SetResult(e.Element);
            Page.FileChooser -= EventHandler;
        }
        Page.FileChooser += EventHandler;
        var chooser = await TaskUtils.WhenAll(
            chooserTsc.Task,
            Page.ClickAsync("input")
        );
        Assert.NotNull(chooser);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should emit event on/off")]
    [Ignore("We don't need to test this")]
    public void ShouldEmitEventOnOff()
    {
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should emit addListener/removeListener")]
    [Ignore("We don't need to test this")]
    public void ShouldEmitEventAddListenerRemoveListener()
    {
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is attached to DOM")]
    public async Task ShouldWorkWhenFileInputIsAttachedToDOM()
    {
        await Page.SetContentAsync("<input type=file>");
        var (chooser, _) = await TaskUtils.WhenAll(
            Page.WaitForFileChooserAsync(),
            Page.ClickAsync("input")
        );
        Assert.NotNull(chooser?.Element);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is not attached to DOM")]
    public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM()
    {
        var (chooser, _) = await TaskUtils.WhenAll(
            Page.WaitForFileChooserAsync(),
            Page.EvaluateAsync(@"() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }")
        );
        Assert.NotNull(chooser?.Element);
    }


    [PlaywrightTest("page-set-input-files.spec.ts", "should work with CSP")]
    public async Task ShouldWorkWithCSP()
    {
        Server.SetCSP("/empty.html", "default-src \"none\"");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<input type=file>");

        await Page.SetInputFilesAsync("input", Path.Combine(Directory.GetCurrentDirectory(), "Assets", FileToUpload));
        Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
        Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should respect timeout")]
    public Task ShouldRespectTimeout()
    {
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(()
         => Page.WaitForFileChooserAsync(new() { Timeout = 1 }));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should respect default timeout when there is no custom timeout")]
    public Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
    {
        Page.SetDefaultTimeout(1);
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForFileChooserAsync());
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should prioritize exact timeout over default timeout")]
    public Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
    {
        Page.SetDefaultTimeout(0);
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForFileChooserAsync(new() { Timeout = 1 }));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        var (chooser, _) = await TaskUtils.WhenAll(
            Page.WaitForFileChooserAsync(new() { Timeout = 0 }),
            Page.EvaluateAsync(@"() => setTimeout(() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }, 50)")
        );
        Assert.NotNull(chooser?.Element);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should return the same file chooser when there are many watchdogs simultaneously")]
    public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously()
    {
        await Page.SetContentAsync("<input type=file>");
        var (fileChooser1, fileChooser2, _) = await TaskUtils.WhenAll(
            Page.WaitForFileChooserAsync(),
            Page.WaitForFileChooserAsync(),
            Page.EvalOnSelectorAsync("input", "input => input.click()")
        );
        Assert.AreEqual(fileChooser1, fileChooser2);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should accept single file")]
    public async Task ShouldAcceptSingleFile()
    {
        await Page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
        var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
        {
            await Page.ClickAsync("input");
        });

        Assert.AreEqual(Page, fileChooser.Page);
        Assert.NotNull(fileChooser.Element);
        await fileChooser.SetFilesAsync(FileToUpload);
        Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
        Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should detect mime type")]
    public async Task ShouldDetectMimeType()
    {
        var files = new List<(string name, string mime, byte[] content)>();

        Server.SetRoute("/upload", context =>
        {
            files.AddRange(context.Request.Form.Files.Select(f =>
            {
                using var ms = new MemoryStream();
                f.CopyTo(ms);
                return (f.FileName, f.ContentType, ms.ToArray());
            }));
            return Task.CompletedTask;
        });

        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
                <form action=""/upload"" method=""post"" enctype=""multipart/form-data"">
                    <input type=""file"" name=""file1"">
                    <input type=""file"" name=""file2"">
                    <input type=""submit"" value=""Submit"">
                </form>
            ");

        await (await Page.QuerySelectorAsync("input[name=file1]")).SetInputFilesAsync(TestUtils.GetAsset("file-to-upload.txt"));
        await (await Page.QuerySelectorAsync("input[name=file2]")).SetInputFilesAsync(TestUtils.GetAsset("pptr.png"));

        await TaskUtils.WhenAll(
           Page.ClickAsync("input[type=submit]"),
           Server.WaitForRequest("/upload")
        );

        Assert.AreEqual("file-to-upload.txt", files[0].name);
        Assert.AreEqual("text/plain", files[0].mime);
        Assert.AreEqual(File.ReadAllBytes(TestUtils.GetAsset("file-to-upload.txt")), files[0].content);

        Assert.AreEqual("pptr.png", files[1].name);
        Assert.AreEqual("image/png", files[1].mime);
        Assert.AreEqual(File.ReadAllBytes(TestUtils.GetAsset("pptr.png")), files[1].content);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should be able to read selected file")]
    public async Task ShouldBeAbleToReadSelectedFile()
    {
        await Page.SetContentAsync("<input type=file>");
        _ = Page.WaitForFileChooserAsync()
            .ContinueWith(task => task.Result.SetFilesAsync(FileToUpload));
        Assert.AreEqual("contents of the file", await Page.EvalOnSelectorAsync<string>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                const reader = new FileReader();
                const promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(picker.files[0]);
                return promise.then(() => reader.result);
            }"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should be able to reset selected files with empty file list")]
    public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList()
    {
        await Page.SetContentAsync("<input type=file>");
        _ = Page.WaitForFileChooserAsync()
            .ContinueWith(task => task.Result.SetFilesAsync(FileToUpload));
        Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        _ = Page.WaitForFileChooserAsync()
            .ContinueWith(task => task.Result.SetFilesAsync(new string[] { }));
        Assert.AreEqual(0, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should not accept multiple files for single-file input")]
    public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
    {
        await Page.SetContentAsync("<input type=file>");
        var (fileChooser, _) = await TaskUtils.WhenAll(
           Page.WaitForFileChooserAsync(),
           Page.ClickAsync("input")
        );
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            fileChooser.SetFilesAsync(new string[]
            {
                    TestUtils.GetAsset(FileToUpload),
                    TestUtils.GetAsset("pptr.png"),
            }));
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should emit input and change events")]
    public async Task ShouldEmitInputAndChangeEvents()
    {
        var events = new List<string>();
        await Page.ExposeFunctionAsync("eventHandled", (string e) => events.Add(e));

        await Page.SetContentAsync(@"
                 <input id=input type=file></input>
                <script>
                  input.addEventListener('input', e => eventHandled(e.type));
                  input.addEventListener('change', e => eventHandled(e.type));
                </script>
            ");

        await (await Page.QuerySelectorAsync("input")).SetInputFilesAsync(TestUtils.GetAsset("file-to-upload.txt"));
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual("input", events[0]);
        Assert.AreEqual("change", events[1]);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should work for single file pick")]
    public async Task ShouldWorkForSingleFilePick()
    {
        await Page.SetContentAsync("<input type=file>");

        var (fileChooser, _) = await TaskUtils.WhenAll(
           Page.WaitForFileChooserAsync(),
           Page.ClickAsync("input")
        );
        Assert.False(fileChooser.IsMultiple);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""multiple""")]
    public async Task ShouldWorkForMultiple()
    {
        await Page.SetContentAsync("<input multiple type=file>");
        var (fileChooser, _) = await TaskUtils.WhenAll(
           Page.WaitForFileChooserAsync(),
           Page.ClickAsync("input")
        );
        Assert.True(fileChooser.IsMultiple);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""webkitdirectory""")]
    public async Task ShouldWorkForWebkitdirectory()
    {
        await Page.SetContentAsync("<input multiple webkitdirectory type=file>");
        var (fileChooser, _) = await TaskUtils.WhenAll(
           Page.WaitForFileChooserAsync(),
           Page.ClickAsync("input")
        );
        Assert.True(fileChooser.IsMultiple);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should upload large file", TestConstants.SlowTestTimeout)]
    public async Task ShouldUploadLargeFile()
    {
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        using var tmpDir = new TempDirectory();
        var filePath = Path.Combine(tmpDir.Path, "200MB");
        using (var stream = File.OpenWrite(filePath))
        {
            var str = new string('a', 4 * 1024);
            for (var i = 0; i < 50 * 1024; i++)
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(str));
            }
        }
        var input = Page.Locator("input[type=file]");
        var events = await input.EvaluateHandleAsync(@"e => {
                const events = [];
                e.addEventListener('input', () => events.push('input'));
                e.addEventListener('change', () => events.push('change'));
                return events;
            }");
        await input.SetInputFilesAsync(filePath);
        Assert.AreEqual(await input.EvaluateAsync<string>("e => e.files[0].name"), "200MB");
        Assert.AreEqual(await events.EvaluateAsync<string[]>("e => e"), new[] { "input", "change" });

        var ((file0Name, file0Size), _) = await TaskUtils.WhenAll(
           Server.WaitForRequest("/upload", request => (request.Form.Files[0].FileName, request.Form.Files[0].Length)),
           Page.ClickAsync("input[type=submit]")
        );
        Assert.AreEqual("200MB", file0Name);
        Assert.AreEqual(200 * 1024 * 1024, file0Size);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should upload multiple large files")]
    public async Task ShouldUploadMultipleLargeFiles()
    {
        var filesCount = 10;
        await Page.GotoAsync(Server.Prefix + "/input/fileupload-multi.html");
        var uploadFile = Path.Combine(Path.GetTempPath(), "50MB_1.zip");
        using (var stream = File.OpenWrite(uploadFile))
        {
            var str = new string('A', 1024);
            // 49 is close to the actual limit
            for (var i = 0; i < 49 * 1024; i++)
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(str));
            }
        }
        var input = Page.Locator("input[type=file]");
        var uploadFiles = new List<string> { uploadFile };
        using var tmpDir = new TempDirectory();
        for (var i = 2; i <= filesCount; i++)
        {
            var dstFile = Path.Combine(tmpDir.Path, $"50MB_{i}.zip");
            File.Copy(uploadFile, dstFile);
            uploadFiles.Add(dstFile);
        }
        var (fileChooser, _) = await TaskUtils.WhenAll(
           Page.WaitForFileChooserAsync(),
           input.ClickAsync()
        );
        await fileChooser.SetFilesAsync(uploadFiles);
        var filesLen = await Page.EvaluateAsync<int>("document.getElementsByTagName(\"input\")[0].files.length");
        Assert.True(fileChooser.IsMultiple);
        Assert.AreEqual(filesCount, filesLen);
        foreach (var file in uploadFiles)
        {
            File.Delete(file);
        }
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should preserve lastModified timestamp")]
    public async Task ShouldPreserveLastModifiedTimestamp()
    {
        await Page.SetContentAsync("<input type=file multiple=true/>");
        var input = Page.Locator("input");
        var files = new string[] { "file-to-upload.txt", "file-to-upload-2.txt" };
        await input.SetInputFilesAsync(files.Select(file => TestUtils.GetAsset(file)));
        Assert.AreEqual(await input.EvaluateAsync<string[]>("e => [...e.files].map(f => f.name)"), files);
        var timestamps = await input.EvaluateAsync<long[]>("e => [...e.files].map(f => f.lastModified)");
        var expectedTimestamps = files.Select(file => new DateTimeOffset(new FileInfo(TestUtils.GetAsset(file)).LastWriteTimeUtc).ToUnixTimeMilliseconds()).ToArray();

        // On Linux browser sometimes reduces the timestamp by 1ms: 1696272058110.0715  -> 1696272058109 or even
        // rounds it to seconds in WebKit: 1696272058110 -> 1696272058000.
        for (var i = 0; i < timestamps.Length; i++)
            Assert.LessOrEqual(Math.Abs(timestamps[i] - expectedTimestamps[i]), 1000);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should upload a folder")]
    public async Task ShouldUploadAFolder()
    {
        await Page.GotoAsync(Server.Prefix + "/input/folderupload.html");
        var input = await Page.QuerySelectorAsync("input");
        using var tmpDir = new TempDirectory();
        var dir = Path.Combine(tmpDir.Path, "file-upload-test");
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "file1.txt"), "file1 content");
            File.WriteAllText(Path.Combine(dir, "file2"), "file2 content");
            Directory.CreateDirectory(Path.Combine(dir, "sub-dir"));
            File.WriteAllText(Path.Combine(dir, "sub-dir", "really.txt"), "sub-dir file content");
        }
        await input.SetInputFilesAsync(dir);
        var webkitRelativePaths = await input.EvaluateAsync<string[]>("e => [...e.files].map(f => f.webkitRelativePath)");
        Assert.True(new HashSet<string> { "file-upload-test/sub-dir/really.txt", "file-upload-test/file1.txt", "file-upload-test/file2" }.SetEquals(new HashSet<string>(webkitRelativePaths)));
        for (var i = 0; i < webkitRelativePaths.Length; i++)
        {
            var content = await input.EvaluateAsync<string>(@"(e, i) => {
                const reader = new FileReader();
                const promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(e.files[i]);
                return promise.then(() => reader.result);
            }", i);
            Assert.AreEqual(File.ReadAllText(Path.Combine(dir, "..", webkitRelativePaths[i])), content);
        }
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should upload a folder and throw for multiple directories")]
    public async Task ShouldUploadAFolderAndThrowForMultipleDirectories()
    {
        await Page.GotoAsync(Server.Prefix + "/input/folderupload.html");
        var input = await Page.QuerySelectorAsync("input");
        using var tmpDir = new TempDirectory();
        var dir = Path.Combine(tmpDir.Path, "file-upload-test");
        {
            Directory.CreateDirectory(Path.Combine(dir, "folder1"));
            File.WriteAllText(Path.Combine(dir, "folder1", "file1.txt"), "file1 content");
            Directory.CreateDirectory(Path.Combine(dir, "folder2"));
            File.WriteAllText(Path.Combine(dir, "folder2", "file2.txt"), "file2 content");
        }
        var ex = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(new string[] { Path.Combine(dir, "folder1"), Path.Combine(dir, "folder2") }));
        Assert.AreEqual("Multiple directories are not supported", ex.Message);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should throw if a directory and files are passed")]
    public async Task ShouldThrowIfADirectoryAndFilesArePassed()
    {
        await Page.GotoAsync(Server.Prefix + "/input/folderupload.html");
        var input = await Page.QuerySelectorAsync("input");
        using var tmpDir = new TempDirectory();
        var dir = Path.Combine(tmpDir.Path, "file-upload-test");
        {
            Directory.CreateDirectory(Path.Combine(dir, "folder1"));
            File.WriteAllText(Path.Combine(dir, "folder1", "file1.txt"), "file1 content");
        }
        var ex = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(new string[] { Path.Combine(dir, "folder1"), Path.Combine(dir, "folder1", "file1.txt") }));
        Assert.AreEqual("File paths must be all files or a single directory", ex.Message);
    }

    [PlaywrightTest("page-set-input-files.spec.ts", "should throw when uploading a folder in a normal file upload input")]
    public async Task ShouldThrowWhenUploadingAFolderInANormalFileUploadInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        var input = await Page.QuerySelectorAsync("input");
        using var tmpDir = new TempDirectory();
        var dir = Path.Combine(tmpDir.Path, "file-upload-test");
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "file1.txt"), "file1 content");
        }
        var ex = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(dir));
        Assert.AreEqual("Error: File input does not support directories, pass individual files instead", ex.Message);
    }
}
