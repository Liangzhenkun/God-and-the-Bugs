# God and the Bugs

> 这是一份始于 2026 年 7 月下旬的精彩回忆，记录了五位因游戏制作结缘的伙伴，在神仙老师的带领下触摸热爱的过程与结果。虽有遗憾，但收获颇丰！
>
> 我们会继续完善这个项目。这些在四季更迭里的小小虫，或许未来有一天能与大众相见！给赵老师递茶一万次！

> A memorable journey that began in late July 2026. This project records five companions brought together by game development, guided by an inspiring mentor as we explored what we love and brought it to life. There were regrets, but even more to gain.
>
> We will keep improving the project. Perhaps these tiny bugs, born through the changing seasons, will one day meet a wider audience! Salute to lovely Teacher Zhao!

## 团队 / Team

| 职责 / Role | 成员 / Members |
| --- | --- |
| 美术 / Art | jiayi, diandian |
| 策划 / Game Design | yifei (Team Leader), sirong |
| 程序 / Programming | zhenkun |
| 支持 / Support | yihui |

## 项目简介 / About

《God and the Bugs》是我们为 GMTK 2026 制作的 Unity 游戏项目。

*God and the Bugs* is our Unity game project for GMTK 2026.

## 下载与安装 / Download & Setup

### 环境要求 / Requirements

- [Unity Hub](https://unity.com/download) 与 **Unity 6000.0.73f1**（Unity 6）
- [Git](https://git-scm.com/downloads)
- [Git LFS](https://git-lfs.com/)（用于下载大型美术、音频等资源）

### 获取项目 / Get the Project

1. 克隆仓库，并拉取 LFS 资源：

   ```bash
   git clone https://github.com/Liangzhenkun/GMTK2026_RAC.git
   cd GMTK2026_RAC
   git lfs pull
   ```

2. 打开 Unity Hub，选择 **Add / 添加**。
3. 选择仓库中的 `GameJame2026` 文件夹，而不是仓库根目录。
4. 使用 Unity **6000.0.73f1** 打开项目；首次导入资源可能需要几分钟。

## 运行与调试 / Run & Debug

1. 在 Unity 的 Project 面板中打开 `Assets/Scenes` 下需要运行的场景。
2. 点击编辑器顶部的 **Play** 按钮运行游戏。
3. 使用 Console 面板查看报错、警告与 `Debug.Log` 输出；双击日志可跳转到对应脚本位置。
4. 若有脚本编译报错，先在 Console 中修复最早出现的错误，再重新进入 Play 模式。

首次打开或资源显示异常时，可确认已执行 `git lfs pull`，然后在 Unity Hub 中重新打开项目。

To run the game, open a scene from `Assets/Scenes` and press **Play** in the Unity editor. Use the **Console** window to inspect errors, warnings, and `Debug.Log` output. If scripts fail to compile, fix the first Console error before entering Play mode again.

## 协作说明 / Collaboration

- 请在分支上开发，避免直接提交到 `main`。
- 功能或修复完成后，请创建 Pull Request 供团队检查。
- 不要提交本地缓存、临时文件、构建产物或导出的包。
- 美术、音频、视频等大型二进制资源请通过 Git LFS 管理。

## 仓库说明 / Repository Notes

本仓库目前为公开仓库。任何人都可以查看和 Fork；只有拥有写入权限的成员可以直接推送分支。

This repository is public. Anyone can view or fork it, while only collaborators with write permission can push branches directly.测试
