using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckyCIPost.Model
{
    public class PostInfo
    {
            public string object_kind { get; set; }
            public string before { get; set; }
            public string after { get; set; }
            public string @ref { get; set; }
            public int user_id { get; set; }
            public string user_name { get; set; }
            public string user_email { get; set; }
            public string user_avatar { get; set; }
            public int project_id { get; set; }
            public project project;
            public repository repository;
            public List<commits> commits;
            public int total_commits_count { get; set; }
        
     
    }
    public class project
    {
        public string name { get; set; }
        public string description { get; set; }
        public string web_url { get; set; }
        public string avatar_url { get; set; }
        public string git_ssh_url { get; set; }
        public string git_http_url { get; set; }
        public string @namespace { get; set; }
        public int visibility_level { get; set; }
        public string path_with_namespace { get; set; }
        public string default_branch { get; set; }
        public string homepage { get; set; }
        public string url { get; set; }
        public string ssh_url { get; set; }
        public string http_url { get; set; }
    }
    public class repository
    {
        public string name { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public string homepage { get; set; }
        public string git_http_url { get; set; }
        public string git_ssh_url { get; set; }
        public int visibility_level { get; set; }
    }
    public class commits
    {
        public string id { get; set; }
        public string message { get; set; }
        public string timestamp { get; set; }
        public string url { get; set; }
        public string added { get; set; }
        public Array modified;
        public Array removed { get; set; }
        public author author;
    }
    public class author
    {
        public string name { get; set; }
        public string email { get; set; }
    }
}