const gulp = require("gulp");
const fs = require("fs");

const watch = () =>
  gulp.watch(
    "/var/watch/*",
    gulp.series(() => {
      console.log("watch detected a change!");
      fs.readdir(testFolder, (err, files) => {
        files.forEach(file => {
          console.log(file);
        });
      });
    })
  );

// gulp.series(watch);
watch();
