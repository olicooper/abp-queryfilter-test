﻿$(function () {
    abp.log.debug('Index.js initialized!');

    var l = abp.localization.getResource('AbpQueryFilterDemo');

    $('#BlogsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.blogs.blog.getList, () => ({
                includeDetails: true,
                useIncludeFilter: false,
                ignoreSoftDelete: false,
                ignoreSoftDeleteForPosts: false // won't apply the filter for Posts
            })),
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
                            strArr.push("<strong>" + p.title + "</strong>");
                        }
                        return strArr.join('<br>');
                    }
                },
            ]
        })
    );

    $('#BlogsTable_IgnoreDeleted').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.blogs.blog.getList, () => ({
                includeDetails: true,
                useIncludeFilter: false,
                ignoreSoftDelete: true,
                ignoreSoftDeleteForPosts: true
            })),
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
                            strArr.push("<strong>" + p.title + "</strong>");
                        }
                        return strArr.join('<br>');
                    }
                },
            ]
        })
    );

    $('#BlogsTable_IgnoreDeletedPosts').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.blogs.blog.getList, () => ({
                includeDetails: true,
                useIncludeFilter: false,
                ignoreSoftDelete: false,
                ignoreSoftDeleteForPosts: true
            })),
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
                            strArr.push("<strong>" + p.title + "</strong>");
                        }
                        return strArr.join('<br>');
                    }
                },
            ]
        })
    );

    $('#PostsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({
                includeDetails: true,
                ignoreSoftDelete: false,
                ignoreSoftDeleteForBlog: false,
                useQuerySyntax: false
            })),
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
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong>") : "undefined")
                },
            ]
        })
    );

    $('#PostsTable_IgnoreDeleted').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({
                includeDetails: true,
                ignoreSoftDelete: true,
                ignoreSoftDeleteForBlog: true,
                useQuerySyntax: false
            })),
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
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong>") : "undefined")
                },
            ]
        })
    );

    $('#PostsTable_IgnoreDeletedBlog').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({
                includeDetails: true,
                ignoreSoftDelete: false,
                ignoreSoftDeleteForBlog: true,
                useQuerySyntax: false
            })),
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
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong>") : "undefined")
                },
            ]
        })
    );

    $('#PostsTable_ExclDetails').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: false,
            order: [[1, "asc"]],
            searching: false,
            ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({
                includeDetails: false,
                ignoreSoftDelete: false,
                ignoreSoftDeleteForBlog: false,
                useQuerySyntax: false
            })),
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
                    render: (b) => (b ? ("<strong>" + b?.name + "</strong>") : "undefined")
                },
            ]
        })
    );

    //$('#PostsTable_ExclDetails2').DataTable(
    //    abp.libs.datatables.normalizeConfiguration({
    //        serverSide: true,
    //        paging: false,
    //        order: [[1, "asc"]],
    //        searching: false,
    //        ajax: abp.libs.datatables.createAjax(abpQueryFilterDemo.posts.post.getList, () => ({
    //            includeDetails: false,
    //            ignoreSoftDelete: false,
    //            ignoreSoftDeleteForBlog: false,
    //            useQuerySyntax: true
    //        })),
    //        columnDefs: [
    //            {
    //                title: l('Post:Title'),
    //                data: "title"
    //            },
    //            {
    //                title: l('IsDeleted'),
    //                data: "isDeleted",
    //                render: (deleted) => (deleted ? "<strong>YES</strong>" : "NO")
    //            },
    //            {
    //                title: l('Blog'),
    //                data: "blog",
    //                render: (b) => (b ? ("<strong>" + b?.name + "</strong>") : "undefined")
    //            },
    //        ]
    //    })
    //);

});