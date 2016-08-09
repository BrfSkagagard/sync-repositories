using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using LibGit2Sharp;

namespace UpdateRepoWithMinolData
{
    class Program
    {
        [DataContract]
        public class Setting
        {
            [DataMember]
            public string Token { get; set; }
            [DataMember]
            public string AuthorName { get; set; }
            [DataMember]
            public string AuthorEmail { get; set; }
        }

        static private string gitFolder = @"C:\Users\Mattias\Documents\GitHub\";

        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                gitFolder = args[0];
            }
            //WriteSettings(gitFolder);
            //return;
            var login = ReadSettings(gitFolder);

            var folders = System.IO.Directory.GetDirectories(gitFolder, "brfskagagard-lgh*");
            Signature author = new Signature(login.AuthorName, login.AuthorEmail, DateTime.Now);

            foreach (string folder in folders)
            {
                SyncRepository(folder, login.Token, author);
            }

            var styrelsenDirectory = new System.IO.DirectoryInfo(gitFolder + "brfskagagard-styrelsen");
            if (styrelsenDirectory.Exists)
            {
                SyncRepository(styrelsenDirectory.FullName, login.Token, author);
            }
        }

        private static void WriteSettings(string gitFolder)
        {
            var stream = System.IO.File.OpenWrite(gitFolder + "git-setting.json");

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(Setting));

            serializer.WriteObject(stream, new Setting
            {
                Token = "...",
                AuthorName = "...",
                AuthorEmail = "..."
            });
            stream.Flush();
            stream.Close();
        }

        private static Setting ReadSettings(string gitFolder)
        {
            var stream = System.IO.File.OpenRead(gitFolder + "git-setting.json");

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(Setting));

            var setting = serializer.ReadObject(stream) as Setting;
            stream.Close();
            return setting;
        }

        private static void SyncRepository(string repositoryFolder, string token, Signature author)
        {
            using (Repository repo = new Repository(repositoryFolder))
            {
                UpdateLocalRepository(repo, token, author);


                AddNewFilesToLocalRepository(repo);

                if (!TryAddChangesToLocalRepository(repo, author))
                {
                    return;
                }

                UpdateRemoteRepository(repo, token);
            }
        }

        private static bool TryAddChangesToLocalRepository(Repository repo, Signature author)
        {
            // Create the committer's signature and commit
            Signature committer = author;
            try
            {
                // Commit to the repository
                Commit commit = repo.Commit("Minol data update", author, committer);
            }
            catch (LibGit2Sharp.EmptyCommitException)
            {
                // There was nothing to commit, so stop here
                return false;
            }
            return true;
        }

        private static void AddNewFilesToLocalRepository(Repository repo)
        {
            repo.Stage("*");
        }

        private static void UpdateRemoteRepository(Repository repo, string token)
        {
            var remote = repo.Network.Remotes["origin"];
            var branch = repo.Branches["master"];
            var pushOptions = new PushOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                {
                    Username = token,
                    Password = ""
                }
            };

            repo.Network.Push(branch, pushOptions);
        }

        private static void UpdateLocalRepository(Repository repo, string token, Signature author)
        {
            Remote remote = repo.Network.Remotes["origin"];
            var options = new FetchOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                {
                    Username = token,
                    Password = ""
                }
            };
            repo.Network.Fetch(remote, options);
            repo.MergeFetchedRefs(author, new MergeOptions()
            {
                FailOnConflict = true
            });
        }
    }
}
