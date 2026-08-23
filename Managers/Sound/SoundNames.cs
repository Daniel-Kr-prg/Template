using UnityEngine;

[System.Serializable]
public enum SoundCategory
{
    Music_1,
    Music_2,
    Effects_UI_1,
    Effects_UI_2,
    Effects_Gameplay
}

[System.Serializable]
public enum SoundName
{
    None = -1,
    Meow,
    Boo,
    UI_press_1,
    UI_release_1,
    UI_hover_1,
    UI_back_1,
    UI_confirm_1,
    UI_error_1,
    UI_toggle_1,
    Startup_show_1,
    Startup_hide_1,
    Puzzle_piece_pickup_1,
    Puzzle_piece_drop_1,
    Puzzle_piece_snap_valid_1,
    Puzzle_piece_snap_invalid_1,
    Puzzle_piece_rotate_1,
    Puzzle_piece_collision_small_1,
    Puzzle_piece_collision_medium_1,
    Box_open_1,
    Box_close_1,
    Box_capture_piece_1,
    Box_shake_low_1,
    Box_shake_high_1,
    Box_pour_start_1,
    Box_pour_end_1,
    Board_complete_1,
    Level_unlock_1,
    Title_spawn_1,
    Title_unlock_1,
    Save_autosave_1,
    StickPuzzle_UI_press_1,
    StickPuzzle_Level_select_press_1,
    StickPuzzle_Stick_select_1,
    StickPuzzle_Stick_confirm_1,
    StickPuzzle_Stick_confirm_2,
    StickPuzzle_Star_achieved_1,
    StickPuzzle_Star_achieved_2,
    StickPuzzle_Star_achieved_3,
    StickPuzzle_Stick_place_1,
    StickPuzzle_Layer_complete_1,
    StickPuzzle_Level_locked_press_1,
    StickPuzzle_Stick_select_2,
    StickPuzzle_Stick_select_3,
    StickPuzzle_Stick_confirm_3,
    StickPuzzle_Stick_deselect_1
}
