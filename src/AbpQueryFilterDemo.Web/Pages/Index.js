$(function () {
    abp.log.debug('Index.js initialized!');

    var l = abp.localization.getResource('AbpQueryFilterDemo');

    var blogsTable = $('#BlogsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.blogs.blog.getList),
            columnDefs: [
                {
                    title: l('Blog:Name'),
                    data: "name"
                },
                {
                    title: l('IsDeleted'),
                    data: "isDeleted",
                    render: (deleted) => (deleted ? "<strong>YES</strong>" : "NO")
                },
                {
                    title: l('Posts'),
                    data: "posts",
                    render: function (data) {
                        var strArr = [];
                        for (var p of data) {
                            //console.log(p)
                            strArr.push("<strong>" + p.title + "</strong>");
                        }
                        return strArr.join('<br>');
                    }
                },
            ]
        })
    );

    var postsTable = $('#PostsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList),
            columnDefs: [
                {
                    title: l('Post:Title'),
                    data: "title"
                },
                {
                    title: l('IsDeleted'),
                    data: "isDeleted",
                    render: (deleted) => (deleted ? "<strong>YES</strong>" : "NO")
                },
                {
                    title: l('Blog'),
                    data: "blog",
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong> > DEL:" + b.isDeleted) : "undefined")
                },
            ]
        })
    );

    var postsTable_ExclDetails = $('#PostsTable_ExclDetails').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({ includeDetails: false })),
            columnDefs: [
                {
                    title: l('Post:Title'),
                    data: "title"
                },
                {
                    title: l('IsDeleted'),
                    data: "isDeleted",
                    render: (deleted) => (deleted ? "<strong>YES</strong>" : "NO")
                },
                {
                    title: l('Blog'),
                    data: "blog",
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong> > DEL:" + b.isDeleted) : "undefined")
                },
            ]
        })
    );

    var postsTable_IgnoreDeleted = $('#PostsTable_IgnoreDeleted').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({ ignoreSoftDelete: true })),
            columnDefs: [
                {
                    title: l('Post:Title'),
                    data: "title"
                },
                {
                    title: l('IsDeleted'),
                    data: "isDeleted",
                    render: (deleted) => (deleted ? "<strong>YES</strong>" : "NO")
                },
                {
                    title: l('Blog'),
                    data: "blog",
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong> > DEL:" + b.isDeleted) : "undefined")
                },
            ]
        })
    );

});